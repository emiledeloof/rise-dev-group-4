using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Payments;
using Rise.Services.Pricing;
using Rise.Shared.Bookings;
using Rise.Shared.Common;
using Rise.Shared.Guests;
using System.Security.Cryptography.X509Certificates;

namespace Rise.Services.Bookings;

/// <summary>
/// Handles booking payment operations.
/// </summary>
public sealed class BookingService(
    ApplicationDbContext dbContext,
    IStripePaymentGateway stripePaymentGateway,
    IConfiguration configuration) : IBookingService
{
    public async Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(
        BookingRequest.CreateCheckoutSession request,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .AsSplitQuery()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
            .ThenInclude(x => x.Rate)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

        if (booking is null)
            return Result.NotFound($"Booking with Id '{request.BookingId}' was not found.");

        if (booking.Rooms.Count == 0)
            return Result.Invalid(new ValidationError("The booking has no rooms."));

        if (booking.Payments.Any(x => x.IsPaid))
            return Result.Conflict("The booking has already been paid.");

        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;
        var amount = booking.Rooms.Sum(x => PriceCalculator.Total(x.PricePerNight, nights));

        if (amount <= 0)
            return Result.Invalid(new ValidationError("The booking amount must be greater than zero."));

        var payment = booking.Payments
            .SingleOrDefault(x => !x.IsPaid && string.IsNullOrWhiteSpace(x.StripeCheckoutSession));

        if (payment is null)
            payment = booking.AddPayment(amount);

        if (payment.Id == 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        var stripeSession = await stripePaymentGateway.CreateCheckoutSessionAsync(
            new StripeCheckoutSessionRequest(
                booking.Id.ToString(),
                payment.Id.ToString(),
                booking.BookingNumber,
                amount,
                configuration["Stripe:Currency"] ?? "eur",
                configuration["Stripe:SuccessUrl"]
                    ?? throw new InvalidOperationException("Stripe:SuccessUrl is not configured."),
                configuration["Stripe:CancelUrl"]
                    ?? throw new InvalidOperationException("Stripe:CancelUrl is not configured."),
                booking.Guest.Email),
            cancellationToken);

        payment.SetStripeCheckoutSession(stripeSession.SessionId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new BookingResponse.CheckoutSession
        {
            CheckoutUrl = stripeSession.CheckoutUrl,
            SessionId = stripeSession.SessionId
        });
    }

    /// <summary>
    /// Saves the booking when the guest clicks "Pay now": checks the rooms and rates, checks availability,
    /// creates or updates the guest and stores everything in one SaveChanges.
    /// </summary>
    public async Task<Result<BookingResponse.Create>> CreateAsync(BookingRequest.Create request, CancellationToken ctx = default)
    {
        var roomIds = request.Rooms.Select(x => x.RoomId).ToList();

        var rooms = await dbContext.Rooms
            .Include(x => x.Rates)
            .Where(x => !x.IsDeleted && roomIds.Contains(x.Id))
            .ToListAsync(ctx);

        if (rooms.Count != roomIds.Count)
        {
            return Result.NotFound("One or more selected rooms were not found.");
        }

        // pair every room with the rate the guest chose, the rate must belong to that room
        var selections = new List<(Room Room, Rate Rate)>();
        foreach (var selection in request.Rooms)
        {
            var room = rooms.Single(x => x.Id == selection.RoomId);
            var rate = room.Rates.SingleOrDefault(x => x.Id == selection.RateId && !x.IsDeleted);
            if (rate is null)
            {
                return Result.Invalid(new ValidationError(
                    $"The selected rate is not available for room '{room.Name}'."));
            }

            // server side check of the min nb of nights, the search already hides these rates but it can be bypassed
            if (request.CheckOut.DayNumber - request.CheckIn.DayNumber < rate.MinNights)
            {
                return Result.Invalid(new ValidationError(
                    $"Room '{room.Name}' needs at least {rate.MinNights} nights."));
            }

            selections.Add((room, rate));
        }

        // same overlap rule as RoomService.GetAvailableAsync
        // TODO: two simultaneous requests can both pass this check, add a Postgres exclusion constraint
        // or a serializable transaction once bookings get a status (InMemory tests don't support transactions)
        var takenRoomIds = await dbContext.BookingRooms
            .Where(br => roomIds.Contains(br.Room.Id)
                && !br.IsDeleted
                && !br.Booking.IsDeleted
                && br.Booking.CheckIn < request.CheckOut
                && br.Booking.CheckOut > request.CheckIn)
            .Select(br => br.Room.Id)
            .Distinct()
            .ToListAsync(ctx);

        if (takenRoomIds.Count > 0)
        {
            var takenNames = rooms.Where(x => takenRoomIds.Contains(x.Id)).Select(x => x.Name);
            return Result.Conflict(
                $"{string.Join(", ", takenNames)} is no longer available for these dates.");
        }

        var guest = await UpsertGuestAsync(request.Guest, ctx);

        var bookingNumber = await GenerateBookingNumberAsync(ctx);
        var booking = new Booking(bookingNumber, guest, request.CheckIn, request.CheckOut);

        foreach (var (room, rate) in selections)
        {
            // the booking copies the price per night of the rate, so the server price is used, not the client one
            booking.AddRoom(room, rate);
        }

        // guest and booking are saved together, one SaveChanges is atomic
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(ctx);

        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        return Result.Created(new BookingResponse.Create
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            TotalAmount = booking.Rooms.Sum(x => PriceCalculator.Total(x.PricePerNight, nights)),
        });
    }

    /// <summary>
    /// Reuses the guest with the same email and updates the details with the latest ones, or creates a new guest.
    /// </summary>
    private async Task<Guest> UpsertGuestAsync(GuestRequest.Create request, CancellationToken ctx)
    {
        var email = request.Email.Trim();
        var name = $"{request.FirstName.Trim()} {request.LastName.Trim()}";
        var phone = request.PhoneNumber.Trim();
        var address = new GuestAddress(request.AddressLine1, request.AddressLine2, request.Country);

        var guest = await dbContext.Guests.SingleOrDefaultAsync(x => x.Email == email, ctx);
        if (guest is null)
        {
            return new Guest(name, email, phone, address);
        }

        guest.Name = name;
        guest.Phone = phone;
        guest.ChangeAddress(address);
        return guest;
    }

    /// <summary>
    /// Loads one booking with its guest, rooms, rates and payments for the admin detail page.
    /// A deleted booking is treated as not found.
    /// </summary>
    public async Task<Result<BookingDto.AdminDetail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Room)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Rate)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == bookingId && !x.IsDeleted, ctx);

        if (booking is null)
        {
            return Result.NotFound($"Booking with ID '{bookingId}' was not found.");
        }

        return Result.Success(ToAdminDetailDto(booking));
    }

    private static BookingDto.AdminDetail ToAdminDetailDto(Booking booking)
    {
        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;
        var rooms = booking.Rooms
            .Where(x => !x.IsDeleted)
            .Select(x => new BookingDto.AdminRoomDto
            {
                RoomId = x.Room.Id,
                RoomNumber = x.Room.RoomNumber,
                RoomName = x.Room.Name,
                RateName = x.Rate.Name,
                PricePerNight = x.PricePerNight,
                Total = PriceCalculator.Total(x.PricePerNight, nights),
            })
            .OrderBy(x => x.RoomNumber)
            .ToList();
        var payments = booking.Payments.Where(x => !x.IsDeleted).ToList();

        return new BookingDto.AdminDetail
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            CreatedAt = booking.CreatedAt,
            // same rule as GetIndexAsync
            Status = payments.Any(x => x.IsPaid) ? "Paid" : "Pending",
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Nights = nights,
            TotalAmount = rooms.Sum(x => x.Total),
            Guest = new BookingDto.AdminGuestDto
            {
                Id = booking.Guest.Id,
                Name = booking.Guest.Name,
                Email = booking.Guest.Email,
                Phone = booking.Guest.Phone,
                AddressLine1 = booking.Guest.Address.AddressLine1,
                AddressLine2 = booking.Guest.Address.AddressLine2,
                Country = booking.Guest.Address.Country,
            },
            Rooms = rooms,
            Payments = payments
                .OrderBy(x => x.Id)
                .Select(x => new BookingDto.AdminPaymentDto
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    PaidAt = x.PaidAt,
                    InvoiceNumber = x.InvoiceNumber,
                    CardBrand = x.CardBrand,
                    CardLast4 = x.CardLast4,
                    StripeReceiptUrl = x.StripeReceiptUrl,
                })
                .ToList(),
        };
    }

    public async Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken ctx = default)
    {
        if (string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return Result.Invalid(new ValidationError("A Stripe Checkout Session is required."));
        }

        var booking = await dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Room)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(
                x => x.Payments.Any(payment => payment.StripeCheckoutSession == checkoutSessionId),
                ctx);

        if (booking is null)
        {
            return Result.NotFound("No booking was found for this Stripe Checkout Session.");
        }

        return Result.Success(ToDetailDto(booking));
    }

    private static BookingDto.Detail ToDetailDto(Booking booking)
    {
        return new BookingDto.Detail
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Guest = new BookingDto.GuestDto
            {
                Id = booking.Guest.Id,
                Name = booking.Guest.Name,
                Email = booking.Guest.Email,
                Phone = booking.Guest.Phone,
            },
            Rooms = booking.Rooms.Select(x => new BookingDto.BookingRoomDto
            {
                Id = x.Id,
                RoomId = x.Room.Id,
                RoomNumber = x.Room.RoomNumber,
                PricePerNight = x.PricePerNight,
            }).ToList(),
            Payments = booking.Payments.Select(x => new BookingDto.PaymentDto
            {
                Id = x.Id,
                Amount = x.Amount,
                PaidAt = x.PaidAt,
            }).ToList(),
        };
    }

    private async Task<string> GenerateBookingNumberAsync(CancellationToken ctx)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        while (true)
        {
            var suffix = RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6", CultureInfo.InvariantCulture);
            var bookingNumber = $"{datePrefix}-SVM-{suffix}";

            if (!await dbContext.Bookings.AnyAsync(x => x.BookingNumber == bookingNumber, ctx))
            {
                return bookingNumber;
            }
        }
    }

    /// <summary>
    /// Returns a paginated list of all bookings for the admin overview.
    /// Status is "Paid" when at least one payment has been settled, otherwise "Pending".
    /// </summary>
    public async Task<Result<BookingResponse.Index>> GetIndexAsync(
        QueryRequest.SkipTake request,
        CancellationToken ctx = default)
    {
        var query = dbContext.Bookings.Where(b => !b.IsDeleted);

        var totalCount = await query.CountAsync(ctx);

        query = request.OrderBy switch
        {
            "CheckIn" => request.OrderDescending
                ? query.OrderByDescending(b => b.CheckIn)
                : query.OrderBy(b => b.CheckIn),
            "CheckOut" => request.OrderDescending
                ? query.OrderByDescending(b => b.CheckOut)
                : query.OrderBy(b => b.CheckOut),
            "GuestName" => request.OrderDescending
                ? query.OrderByDescending(b => b.Guest.Name)
                : query.OrderBy(b => b.Guest.Name),
            _ => query.OrderByDescending(b => b.Id), // default: newest first
        };

        var rows = await query
            .AsNoTracking()
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(b => new
            {
                b.Id,
                b.BookingNumber,
                b.CheckIn,
                b.CheckOut,
                GuestName = b.Guest.Name,
                GuestEmail = b.Guest.Email,
                RoomPrices = b.Rooms.Select(r => r.PricePerNight).ToList(),
                RoomNumbers = b.Rooms.Select(r => r.Room.RoomNumber).ToList(),
                Status = b.Payments.Any(p => p.PaidAt != null) ? "Paid" : "Pending",
            })
            .ToListAsync(ctx);

        // PriceCalculator can't be translated to SQL, so the total is calculated after loading
        var bookings = rows.Select(x =>
        {
            var nights = x.CheckOut.DayNumber - x.CheckIn.DayNumber;
            return new BookingDto.Index
            {
                Id = x.Id,
                BookingNumber = x.BookingNumber,
                CheckIn = x.CheckIn,
                CheckOut = x.CheckOut,
                GuestName = x.GuestName,
                GuestEmail = x.GuestEmail,
                TotalAmount = x.RoomPrices.Sum(p => PriceCalculator.Total(p, nights)),
                RoomNumbers = x.RoomNumbers,
                Status = x.Status,
            };
        }).ToList();

        return Result.Success(new BookingResponse.Index
        {
            Bookings = bookings,
            TotalCount = totalCount,
        });
    }

    /// <summary>
    /// Returns all rooms with the bookings that overlap the requested date window,
    /// used to render the admin booking matrix calendar.
    /// </summary>
    public async Task<Result<BookingResponse.Matrix>> GetMatrixAsync(
        BookingRequest.Matrix request,
        CancellationToken ctx = default)
    {
        var windowEnd = request.WindowStart.AddDays(request.WindowDays);

        // Load all non-deleted rooms ordered by name
        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.RoomNumber, r.Name })
            .ToListAsync(ctx);

        // Load all paid booking-room entries that fall inside the window in one query,
        // navigating from the owning side (BookingRoom → Booking, Booking → Guest/Payments).
        // Pending (unpaid) bookings are excluded from the matrix.
        var overlapping = await dbContext.BookingRooms
            .AsNoTracking()
            .Where(br => !br.IsDeleted
                      && !br.Booking.IsDeleted
                      && br.Booking.Payments.Any(p => p.PaidAt != null)
                      && br.Booking.CheckIn < windowEnd
                      && br.Booking.CheckOut > request.WindowStart)
            .Select(br => new
            {
                RoomId = br.Room.Id,
                br.Booking.Id,
                br.Booking.BookingNumber,
                GuestName = br.Booking.Guest.Name,
                br.Booking.CheckIn,
                br.Booking.CheckOut,
                Status = "Paid",
            })
            .ToListAsync(ctx);

        // Group bookings by room and build the response
        var bookingsByRoom = overlapping
            .GroupBy(br => br.RoomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = rooms.Select(r =>
        {
            var roomBookings = bookingsByRoom.TryGetValue(r.Id, out var brs) ? brs : [];
            return new BookingDto.MatrixRow
            {
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                RoomName = r.Name,
                Bookings = roomBookings.Select(b => new BookingDto.MatrixBooking
                {
                    Id = b.Id,
                    BookingNumber = b.BookingNumber,
                    GuestName = b.GuestName,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    Status = b.Status,
                }).ToList(),
            };
        }).ToList();

        return Result.Success(new BookingResponse.Matrix
        {
            WindowStart = request.WindowStart,
            WindowDays = request.WindowDays,
            Rows = rows,
        });
    }
}

