using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Services.Payments;
using Rise.Shared.Bookings;

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
        var amount = booking.Rooms.Sum(x => x.PricePerNight * nights);

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
    
    public async Task<Result<BookingResponse.Create>> CreateAsync(BookingRequest.Create request, CancellationToken ctx = default)
    {
        var guest = await dbContext.Guests.SingleOrDefaultAsync(x => x.Id == request.GuestId, ctx);
        if (guest is null)
        {
            return Result.NotFound($"Guest with ID '{request.GuestId}' was not found.");
        }

        var rooms = await dbContext.Rooms
            .Include(x => x.Rates)
            .Where(x => request.RoomIds.Contains(x.Id))
            .ToListAsync(ctx);

        if (rooms.Count != request.RoomIds.Count)
        {
            return Result.NotFound("One or more selected rooms were not found.");
        }

        var bookingNumber = await GenerateBookingNumberAsync(ctx);
        var booking = new Booking(bookingNumber, guest, request.CheckIn, request.CheckOut);

        foreach (var room in rooms)
        {
            var rate = room.Rates.FirstOrDefault();
            if (rate is null)
            {
                return Result.Error($"Room '{room.RoomNumber}' does not have a rate configured.");
            }

            booking.AddRoom(room, rate);
        }

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(ctx);

        return Result.Created(new BookingResponse.Create
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
        });
    }

    public async Task<Result<BookingDto.Detail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Room)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == bookingId, ctx);

        if (booking is null)
        {
            return Result.NotFound($"Booking with ID '{bookingId}' was not found.");
        }

        return Result.Success(new BookingDto.Detail
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
        });
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
}
