using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Services.Rooms;

/// <summary>
/// room features, reads the rooms and bookings from the db
/// same idea as InvoiceService
/// </summary>
public class RoomService(ApplicationDbContext dbContext) : IRoomService
{
    /// <summary>
    /// rooms with no booking overlapping the given stay
    /// </summary>
    public async Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        // the endpoint already ran the StayPeriod validator so both dates are filled here
        // take them out of the nullable with .Value, ! tells the compiler we know it's not null
        var checkIn = request.CheckIn!.Value;
        var checkOut = request.CheckOut!.Value;

        // start from the rooms that are not deleted
        // keep a room only if NO booking of that room overlaps the stay
        // overlap rule, an existing booking overlaps when it starts before our check out AND ends after our check in
        // ignore deleted bookings
        // TODO: also ignore cancelled bookings once the domain has a status
        var query = dbContext.Rooms.Where(r => !r.IsDeleted);
        query = query.Where(r => !dbContext.BookingRooms.Any(br =>
            br.Room.Id == r.Id
            && !br.IsDeleted
            && !br.Booking.IsDeleted
            && br.Booking.CheckIn < checkOut
            && br.Booking.CheckOut > checkIn));

        // turn each room into a RoomDto.Available, price from the first rate and url from the first photo
        var rooms = await query.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto.Available
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                MaxGuests = r.MaxGuests,
                PricePerNight = r.Rates.Select(x => x.PricePerNight).FirstOrDefault(),
                PhotoUrl = r.Photos.Select(p => p.Url).FirstOrDefault()
            })
            .ToListAsync(ctx);

        // wrap the list in a RoomResponse.Available and send it back as a success
        return Result.Success(new RoomResponse.Available
        {
            Rooms = rooms
        });
    }
}
