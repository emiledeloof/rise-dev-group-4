using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Rooms;
using Rise.Shared.Search;
using Rise.Services.Bookings;
using Rise.Services.Common;
using Rise.Services.Pricing;
using Rise.Shared.Amenities;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;

namespace Rise.Services.Rooms;

/// <summary>
/// room features, reads the rooms and bookings from the db
/// </summary>
public class RoomService(ApplicationDbContext dbContext, ILanguageProvider languageProvider) : IRoomService
{
    // starting point of every query, the rooms that are not deleted, read only
    private IQueryable<Room> ActiveRooms => dbContext.Rooms.AsNoTracking().Where(r => !r.IsDeleted);

    /// <summary>
    /// rooms free for the whole stay, each with all its rates
    /// a rate that needs more nights than the stay is still sent with its MinNights so the guest sees why it cant be picked
    /// </summary>
    public async Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        // dates already checked by the StayPeriod validator, so never null here
        var checkIn = request.CheckIn!.Value;
        var checkOut = request.CheckOut!.Value;
        var lastNight = checkOut.AddDays(-1);
        var nights = request.Nights;
        var settings = await dbContext.GeneralSettings
            .AsNoTracking()
            .SingleAsync(item => item.Id == GeneralSettings.SingletonId, ctx);

        var totalCapacity = await ActiveRooms.SumAsync(r => r.MaxGuests, ctx);
        var totalRooms = await ActiveRooms.CountAsync(ctx);

        // if requested rooms exceeds total estate rooms, return empty rooms
        if (request.Rooms > totalRooms)
        {
            return Result.Success(new RoomResponse.Available
            {
                Rooms = [],
                TotalCapacity = totalCapacity,
                TotalRooms = totalRooms
            });
        }

        // rooms with a booking during the stay, EF turns this into a subquery
        var bookedRoomIds = dbContext.BookingRooms
            .Overlapping(checkIn, checkOut)
            .Select(br => br.Room.Id);

        var rows = await ActiveRooms
            .Where(r => !bookedRoomIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.MaxGuests,
                // same order as the detail page
                PhotoUrl = r.Photos
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Id)
                    .Select(p => p.Url)
                    .FirstOrDefault(),
                // only the rates valid on the stay dates
                // the rates that fit the stay first, then the ones that need more nights, cheapest first in each group
                // so the 1st rate is the cheapest one the guest can book
                Rates = r.Rates
                    .Where(x => !x.IsDeleted
                        && x.IsActive
                        && (x.StartDate == null || x.StartDate <= checkIn)
                        && (x.EndDate == null || x.EndDate >= lastNight))
                    .OrderBy(x => x.MinNights > nights)
                    .ThenBy(x => x.PricePerNight)
                    .Select(x => new RateDto.Available
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        PricePerNight = x.PricePerNight,
                        TotalPrice = PriceCalculator.Total(x.PricePerNight, nights),
                        // shared cant see the enum so a bool
                        IsRefundable = x.CancellationPolicy == CancellationPolicy.Refundable,
                        FullRefundDays = x.FullRefundDays,
                        PartialRefundDays = x.PartialRefundDays,
                        PartialRefundPercent = x.PartialRefundPercent,
                        MinNights = x.MinNights,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate
                    }).ToList()
            })
            .ToListAsync(ctx);

        var appliesVat = settings.IsVatEnabled && settings.VatPercentage > 0m;

        foreach (var rate in rows.SelectMany(row => row.Rates))
        {
            rate.PricePerNight = PriceCalculator.InclusiveOfVat(
                rate.PricePerNight,
                settings.IsVatEnabled,
                settings.VatPercentage);
            rate.TotalPrice = PriceCalculator.Total(rate.PricePerNight, nights);
            // from the total paid like the confirmation and the bill, the rounded VAT per night would make them differ
            rate.TotalPriceExclVat = appliesVat ? PriceCalculator.ExclusiveOfVat(rate.TotalPrice, settings.VatPercentage) : null;
        }

        // a room without a rate valid on the stay dates cant be booked, so it is left out
        // from price = 1st rate, ie the cheapest bookable for the stay, or the cheapest one when none fits
        var rooms = rows
            .Where(r => r.Rates.Count > 0)
            .Select(r => new RoomDto.Available
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                MaxGuests = r.MaxGuests,
                PricePerNight = r.Rates[0].PricePerNight,
                PhotoUrl = r.PhotoUrl,
                Nights = nights,
                TotalPrice = r.Rates[0].TotalPrice,
                Rates = r.Rates
            }).ToList();

        return Result.Success(new RoomResponse.Available
        {
            Rooms = rooms,
            TotalCapacity = totalCapacity,
            TotalRooms = totalRooms
        });
    }

    /// <summary>
    /// one room for the detail page, NotFound when the id is unknown or the room is deleted
    /// </summary>
    public async Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        var row = await ActiveRooms
            .Where(r => r.Id == roomId)
            .Select(r => new
            {
                Detail = new RoomDto.Detail
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
                    View = r.View,
                    Policies = r.Policies,
                },
                // the beds and amenities belong to the room type, shared by all its rooms
                // all languages are read, the one of the guest is picked below with the default language as fallback
                Beds = r.RoomType.Translations
                    .Select(t => new { t.Language, t.BedConfiguration })
                    .ToList(),
                // an inactive amenity stays assigned to the type but is hidden from the guest
                Amenities = r.RoomType.Amenities
                    .Where(a => !a.IsDeleted && a.IsActive)
                    .Select(a => new
                    {
                        a.Icon,
                        Names = a.Translations.Select(t => new { t.Language, t.Name }).ToList(),
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync(ctx);

        if (row is null)
            return Result.NotFound($"Room with ID '{roomId}' was not found.");

        var language = languageProvider.Language;
        var room = row.Detail;
        room.BedConfiguration = Translations.Pick(row.Beds, b => b.Language, b => b.BedConfiguration, language);
        room.Amenities = row.Amenities
            .Select(a => new AmenityDto.Index
            {
                Name = Translations.Pick(a.Names, n => n.Language, n => n.Name, language)!,
                Icon = a.Icon,
            })
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return Result.Success(room);
    }

    /// <summary>
    /// the first rooms for the landing page, at most request.Take, with the total nb of rooms
    /// </summary>
    public async Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default)
    {
        // all the rooms, not only the ones sent back, so the page can link to the others
        var totalCount = await ActiveRooms.CountAsync(ctx);
        var totalCapacity = await ActiveRooms.SumAsync(r => r.MaxGuests, ctx);

        // the rooms are seeded in display order, so the id keeps that order
        var rooms = await ActiveRooms
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
                    .Where(x => !x.IsDeleted && x.IsActive)
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
