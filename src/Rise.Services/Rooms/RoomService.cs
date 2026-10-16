using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Rooms;
using Rise.Shared.Search;
using Rise.Services.Pricing;

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

        // nb of nights, same calculation as the search page
        var nights = request.Nights;

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

        // turn each room into a RoomDto.Available, "from" price = cheapest rate, all rates listed, url from the first photo
        var rooms = await query.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto.Available
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                MaxGuests = r.MaxGuests,
                PricePerNight = r.Rates.OrderBy(x => x.PricePerNight).Select(x => x.PricePerNight).FirstOrDefault(),
                PhotoUrl = r.Photos.Select(p => p.Url).FirstOrDefault(),
                Nights = nights,
                TotalPrice = PriceCalculator.Total(r.Rates.OrderBy(x => x.PricePerNight).Select(x => x.PricePerNight).FirstOrDefault(), nights),
                Rates = r.Rates
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.PricePerNight)
                    .Select(x => new RateDto.Available
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        PricePerNight = x.PricePerNight,
                        TotalPrice = PriceCalculator.Total(x.PricePerNight, nights)
                    }).ToList()
            })
            .ToListAsync(ctx);

        // wrap the list in a RoomResponse.Available and send it back as a success
        return Result.Success(new RoomResponse.Available
        {
            Rooms = rooms
        });
    }

    /// <summary>
    /// one room for the detail page, NotFound when the id is unknown or the room is deleted
    /// </summary>
    public async Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        var room = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId && !r.IsDeleted)
            .Select(r => new RoomDto.Detail
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                MaxGuests = r.MaxGuests,
                // RoomPhoto has no position yet, the photos are added in display order so the id keeps that order
                PhotoUrls = r.Photos
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Id)
                    .Select(p => p.Url)
                    .ToList(),
                SizeInSquareMeters = r.SizeInSquareMeters,
                BedType = r.BedType,
                View = r.View,
                Amenities = r.Amenities
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Name)
                    .Select(a => new AmenityDto.Index { Name = a.Name, Icon = a.Icon })
                    .ToList()
            })
            .SingleOrDefaultAsync(ctx);

        if (room is null)
            return Result.NotFound($"Room with ID '{roomId}' was not found.");

        return Result.Success(room);
    }
}
