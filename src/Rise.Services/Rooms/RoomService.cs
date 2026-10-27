using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Rooms;
using Rise.Shared.Search;
using Rise.Services.Pricing;
using Rise.Domain.Rooms;

namespace Rise.Services.Rooms;

/// <summary>
/// room features, reads the rooms and bookings from the db
/// same idea as InvoiceService
/// </summary>
public class RoomService(ApplicationDbContext dbContext) : IRoomService
{
    /// <summary>
    /// rooms free for the whole stay, each with the rates the guest can book for that nb of nights
    /// </summary>
    public async Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        // dates already checked by the StayPeriod validator, so never null here
        var checkIn = request.CheckIn!.Value;
        var checkOut = request.CheckOut!.Value;
        var nights = request.Nights;

        // total resort capacity dynamically from the database
        var totalEstateCapacity = await dbContext.Rooms.Where(r => !r.IsDeleted).SumAsync(r => r.MaxGuests, ctx);

        // rooms not deleted
        var query = dbContext.Rooms.Where(r => !r.IsDeleted);

        // keep only the rooms with at least 1 rate bookable for this nb of nights
        query = query.Where(r => r.Rates.Any(x => !x.IsDeleted && x.MinNights <= nights));
        // no booking of the room overlaps the stay, ie starts before our check out and ends after our check in
        // TODO: also ignore cancelled bookings once the domain has a status
        query = query.Where(r => !dbContext.BookingRooms.Any(br =>
            br.Room.Id == r.Id
            && !br.IsDeleted
            && !br.Booking.IsDeleted
            && br.Booking.CheckIn < checkOut
            && br.Booking.CheckOut > checkIn));

        // from price = cheapest rate, photo = 1st one
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
                // only the rates whose min nb of nights fits the stay, cheapest first
                Rates = r.Rates
                    .Where(x => !x.IsDeleted && x.MinNights <= nights)
                    .OrderBy(x => x.PricePerNight)
                    .Select(x => new RateDto.Available
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        PricePerNight = x.PricePerNight,
                        TotalPrice = PriceCalculator.Total(x.PricePerNight, nights),
                        // * cancellation policy shown to the guest, shared cant see the enum so a bool
                        IsRefundable = x.CancellationPolicy == CancellationPolicy.Refundable,
                        FullRefundDays = x.FullRefundDays,
                        PartialRefundDays = x.PartialRefundDays,
                        PartialRefundPercent = x.PartialRefundPercent
                    }).ToList()
            })
            .ToListAsync(ctx);

        return Result.Success(new RoomResponse.Available
        {
            Rooms = rooms,
            TotalCapacity = totalEstateCapacity
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
                Policies = r.Policies,
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

    /// <summary>
    /// the first rooms for the landing page, at most request.Take, with the total nb of rooms
    /// </summary>
    public async Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default)
    {
        var query = dbContext.Rooms.AsNoTracking().Where(r => !r.IsDeleted);

        // all the rooms, not only the ones sent back, so the page can link to the others
        var totalCount = await query.CountAsync(ctx);
        var totalCapacity = await query.SumAsync(r => r.MaxGuests, ctx);

        // the rooms are seeded in display order, so the id keeps that order
        var rooms = await query
            .OrderBy(r => r.Id)
            .Take(request.Take)
            .Select(r => new RoomDto.Summary
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                MaxGuests = r.MaxGuests,
                // "from" price = cheapest rate, the cast to decimal? gives null when the room has no rate instead of 0
                FromPrice = r.Rates
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.PricePerNight)
                    .Select(x => (decimal?)x.PricePerNight)
                    .FirstOrDefault(),
                // same order as the detail page
                PhotoUrls = r.Photos
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Id)
                    .Select(p => p.Url)
                    .ToList()
            })
            .ToListAsync(ctx);

        return Result.Success(new RoomResponse.List
        {
            Rooms = rooms,
            TotalCount = totalCount,
            TotalCapacity = totalCapacity
        });
    }
}
