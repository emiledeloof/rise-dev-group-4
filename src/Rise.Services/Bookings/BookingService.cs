using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;
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

        if (booking.IsCancelled)
            return Result.Conflict("The booking has been cancelled.");

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
            var rate = room.Rates.SingleOrDefault(x => x.Id == selection.RateId && !x.IsDeleted && x.IsActive);
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

            var lastNight = request.CheckOut.AddDays(-1);
            if ((rate.StartDate is not null && rate.StartDate > request.CheckIn)
                || (rate.EndDate is not null && rate.EndDate < lastNight))
            {
                return Result.Invalid(new ValidationError(
                    $"The selected rate is not available for room '{room.Name}' on these dates."));
            }

            selections.Add((room, rate));
        }

        // server side check of the nb of guests, the search already hides rooms that are too small but it can be bypassed
        var capacity = selections.Sum(x => x.Room.MaxGuests);
        if (request.Adults > capacity)
        {
            return Result.Invalid(new ValidationError(
                $"The selected rooms fit at most {capacity} guests."));
        }

        // TODO: two simultaneous requests can both pass this check, add a Postgres exclusion constraint
        // or a serializable transaction (InMemory tests don't support transactions)
        var takenRoomIds = await dbContext.BookingRooms
            .Overlapping(request.CheckIn, request.CheckOut)
            .Where(br => roomIds.Contains(br.Room.Id))
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
        var settings = await dbContext.GeneralSettings
            .AsNoTracking()
            .SingleAsync(item => item.Id == GeneralSettings.SingletonId, ctx);

        var bookingNumber = await GenerateBookingNumberAsync(ctx);
        var booking = new Booking(bookingNumber, guest, request.CheckIn, request.CheckOut, request.Adults);

        foreach (var (room, rate) in selections)
        {
            var pricePerNight = PriceCalculator.InclusiveOfVat(
                rate.PricePerNight,
                settings.IsVatEnabled,
                settings.VatPercentage);
            // the VAT pct is kept on the room, so the bill still shows the right VAT after the setting changes
            booking.AddRoom(room, rate, pricePerNight, settings.IsVatEnabled ? settings.VatPercentage : 0m);
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
    /// Loads one booking with its guest, rooms and payments for the admin detail page.
    /// The rate and cancellation policy come from the copy on the booked room, not from the current rate.
    /// A deleted booking is treated as not found, a cancelled one is still returned.
    /// </summary>
    public async Task<Result<BookingDto.AdminDetail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Room)
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // a cancelled booking shows the refund of the day it was cancelled, an open one the refund of today
        var referenceDate = booking.CancelledAt is { } cancelled ? DateOnly.FromDateTime(cancelled) : today;
        var bookedRooms = booking.Rooms.Where(x => !x.IsDeleted).ToList();
        var rooms = bookedRooms
            .Select(x =>
            {
                var isRefundable = x.CancellationPolicy is CancellationPolicy.Refundable;
                return new BookingDto.AdminRoomDto
                {
                    RoomId = x.Room.Id,
                    RoomNumber = x.Room.RoomNumber,
                    RoomName = x.Room.Name,
                    RateName = x.RateName,
                    PricePerNight = x.PricePerNight,
                    Total = PriceCalculator.Total(x.PricePerNight, nights),
                    IsRefundable = isRefundable,
                    FullRefundDays = x.FullRefundDays,
                    PartialRefundDays = x.PartialRefundDays,
                    PartialRefundPercent = x.PartialRefundPercent,
                    FullRefundUntil = isRefundable ? booking.CheckIn.AddDays(-x.FullRefundDays) : null,
                    PartialRefundUntil = isRefundable ? booking.CheckIn.AddDays(-x.PartialRefundDays) : null,
                    RefundPercent = RefundCalculator.RefundPercent(x, booking.CheckIn, referenceDate),
                    RefundAmount = RoomRefund(x, booking.CheckIn, nights, referenceDate),
                };
            })
            .OrderBy(x => x.RoomNumber)
            .ToList();
        var payments = booking.Payments.Where(x => !x.IsDeleted).ToList();
        var paidAmount = booking.PaidAmount;

        return new BookingDto.AdminDetail
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            CreatedAt = booking.CreatedAt,
            Status = BookingStatusRule.For(booking),
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Nights = nights,
            Adults = booking.Adults,
            TotalAmount = rooms.Sum(x => x.Total),
            PaidAmount = paidAmount,
            SuggestedRefundAmount = Math.Min(bookedRooms.Sum(x => RoomRefund(x, booking.CheckIn, nights, today)), paidAmount),
            RefundReferenceDate = referenceDate,
            Cancellation = booking.CancelledAt is { } cancelledAt
                ? new BookingDto.AdminCancellationDto
                {
                    CancelledAt = cancelledAt,
                    Reason = booking.CancellationReason!,
                    RefundAmount = booking.RefundAmount ?? 0,
                }
                : null,
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

    /// <summary>
    /// Refund of one room according to the policy copied at booking time, as if the booking is cancelled on the given date.
    /// The page and the cancel form both use it, so their amounts always match.
    /// </summary>
    private static decimal RoomRefund(BookingRoom bookingRoom, DateOnly checkIn, int nights, DateOnly cancelledOn)
        => Math.Round(
            PriceCalculator.Total(bookingRoom.PricePerNight, nights) * RefundCalculator.RefundPercent(bookingRoom, checkIn, cancelledOn) / 100m,
            2);

    /// <summary>
    /// Cancels the booking and records the refund the admin decided on.
    /// ! no refund is executed here, the admin pays it back by hand in Stripe
    /// </summary>
    public async Task<Result<BookingResponse.Cancel>> CancelAsync(BookingRequest.Cancel request, CancellationToken ctx = default)
    {
        var booking = await dbContext.Bookings
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == request.BookingId && !x.IsDeleted, ctx);

        if (booking is null)
        {
            return Result.NotFound($"Booking with ID '{request.BookingId}' was not found.");
        }

        if (booking.IsCancelled)
        {
            return Result.Conflict($"Booking {booking.BookingNumber} is already cancelled.");
        }

        if (booking.IsSettled)
        {
            return Result.Conflict($"Booking {booking.BookingNumber} is settled, it can't be cancelled.");
        }

        if (request.RefundAmount > booking.PaidAmount)
        {
            return Result.Invalid(new ValidationError(
                $"The refund can't be more than the amount paid (€{booking.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture)})."));
        }

        booking.Cancel(DateTime.UtcNow, request.Reason, request.RefundAmount);
        await dbContext.SaveChangesAsync(ctx);

        return Result.Success(new BookingResponse.Cancel
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            CancelledAt = booking.CancelledAt!.Value,
            RefundAmount = request.RefundAmount,
        });
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
        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;

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
            Rooms = booking.Rooms.Select(x =>
            {
                var total = PriceCalculator.Total(x.PricePerNight, nights);
                return new BookingDto.BookingRoomDto
                {
                    Id = x.Id,
                    RoomId = x.Room.Id,
                    RoomNumber = x.Room.RoomNumber,
                    RoomName = x.Room.Name,
                    PricePerNight = x.PricePerNight,
                    Total = total,
                    // from the VAT pct stored at booking time, so a later VAT change doesn't alter the breakdown
                    TotalExclVat = x.VatPercentage > 0m ? PriceCalculator.ExclusiveOfVat(total, x.VatPercentage) : null,
                };
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
    /// Returns a paginated list of the bookings for the admin overview, optionally filtered on cancellation.
    /// Status is "Cancelled" for a cancelled booking, else "Paid" when at least one payment has been settled, otherwise "Pending".
    /// </summary>
    public async Task<Result<BookingResponse.Index>> GetIndexAsync(
        BookingRequest.Index request,
        CancellationToken ctx = default)
    {
        var query = dbContext.Bookings.Where(b => !b.IsDeleted);

        query = request.CancellationFilter switch
        {
            BookingCancellationFilters.NotCancelled => query.Where(b => b.CancelledAt == null),
            BookingCancellationFilters.OnlyCancelled => query.Where(b => b.CancelledAt != null),
            _ => query,
        };

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
                IsCancelled = b.CancelledAt != null,
                IsBillSettled = b.SettledAt != null,
                HasSettledPayment = b.Payments.AsQueryable().Any(BookingStatusRule.IsSettled),
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
                Status = BookingStatusRule.For(x.IsCancelled, x.IsBillSettled, x.HasSettledPayment),
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

        // Load all non-deleted booking-room entries that overlap the window in one query,
        // navigating from the owning side (BookingRoom → Booking, Booking → Guest/Payments).
        // Both paid (Occupied) and unpaid (Temporary) bookings are included so the matrix
        // can colour-code them differently. Cancelled bookings are excluded.
        var overlapping = await dbContext.BookingRooms
            .AsNoTracking()
            .Overlapping(request.WindowStart, windowEnd)
            .Select(br => new
            {
                RoomId = br.Room.Id,
                br.Booking.Id,
                br.Booking.BookingNumber,
                GuestName = br.Booking.Guest.Name,
                br.Booking.CheckIn,
                br.Booking.CheckOut,
                IsPaid = br.Booking.Payments.Any(p => p.PaidAt != null),
                IsBillSettled = br.Booking.SettledAt != null,
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
                    Status = b.IsBillSettled ? CellStatus.Settled
                        : b.IsPaid ? CellStatus.Occupied
                        : CellStatus.Temporary,
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
