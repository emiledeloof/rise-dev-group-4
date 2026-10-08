using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Bookings;
using System.Globalization;
using System.Security.Cryptography;

namespace Rise.Services.Bookings;

public class BookingService(ApplicationDbContext dbContext) : IBookingService
{
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