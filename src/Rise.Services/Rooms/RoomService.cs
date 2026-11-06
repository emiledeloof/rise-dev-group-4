using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Rooms;
using Rise.Shared.Search;
using Rise.Services.Bookings;
using Rise.Services.Pricing;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;

namespace Rise.Services.Rooms;

/// <summary>
/// room features, reads the rooms and bookings from the db
/// </summary>
public class RoomService(ApplicationDbContext dbContext) : IRoomService
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

        foreach (var rate in rows.SelectMany(row => row.Rates))
        {
            rate.PricePerNight = PriceCalculator.InclusiveOfVat(
                rate.PricePerNight,
                settings.IsVatEnabled,
                settings.VatPercentage);
            rate.TotalPrice = PriceCalculator.Total(rate.PricePerNight, nights);
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

        // suitable rooms means there exists at least one combination of request.Rooms whose capacity >= request.Adults
        var suitableRooms = rooms.Count >= request.Rooms
            && HasSuitableCombination(rooms, request.Rooms, request.Adults);
        var alternativePeriods = !suitableRooms
            ? await FindAlternativeStayPeriodsAsync(request, settings, ctx)
            : [];

        return Result.Success(new RoomResponse.Available
        {
            Rooms = rooms,
            TotalCapacity = totalCapacity,
            TotalRooms = totalRooms,
            AlternativePeriods = alternativePeriods
        });
    }

    private static bool HasSuitableCombination(IReadOnlyList<RoomDto.Available> rooms, int targetRooms, int targetAdults)
    {
        if (rooms.Count < targetRooms)
            return false;

        return GetCombinations(rooms.ToList(), targetRooms)
            .Any(combo => combo.Sum(r => r.MaxGuests) >= targetAdults);
    }

    private static List<List<T>> GetCombinations<T>(List<T> list, int length)
    {
        if (length == 0)
            return [[]];

        var result = new List<List<T>>();
        for (var i = 0; i <= list.Count - length; i++)
        {
            var head = list[i];
            var tail = list.Skip(i + 1).ToList();
            foreach (var sub in GetCombinations(tail, length - 1))
            {
                result.Add([head, .. sub]);
            }
        }
        return result;
    }

    /// <summary>
    /// finds the next 2 available stay periods of the same length that satisfy the requested room count and capacity,
    /// searching forward from the day after the requested check-in date regardless of distance
    /// </summary>
    private async Task<List<RoomResponse.AlternativeStayPeriodDto>> FindAlternativeStayPeriodsAsync(
        SearchRequest.StayPeriod request,
        GeneralSettings settings,
        CancellationToken ctx)
    {
        var nights = request.Nights;
        var reqRooms = request.Rooms;
        var reqAdults = request.Adults;

        // load all active rooms with their active rates and existing non-cancelled bookings
        var activeRooms = await ActiveRooms
            .Select(r => new
            {
                r.Id,
                r.MaxGuests,
                Rates = r.Rates
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .Select(x => new
                    {
                        x.Id,
                        x.MinNights,
                        x.PricePerNight,
                        x.StartDate,
                        x.EndDate
                    }).ToList(),
                Bookings = dbContext.BookingRooms
                    .Where(br => !br.IsDeleted
                        && !br.Booking.IsDeleted
                        && br.Booking.CancelledAt == null
                        && br.Room.Id == r.Id)
                    .Select(br => new { br.Booking.CheckIn, br.Booking.CheckOut })
                    .ToList()
            })
            .ToListAsync(ctx);

        if (activeRooms.Count < reqRooms)
            return [];

        // start searching forward from the day after the requested checkIn, or tomorrow if requested was in the past
        var today = DateOnly.FromDateTime(DateTime.Today);
        var candidateCheckIn = request.CheckIn!.Value.AddDays(1);
        if (candidateCheckIn < today)
            candidateCheckIn = today;

        var alternatives = new List<RoomResponse.AlternativeStayPeriodDto>();
        const int maxDaysToScan = 730; // scan up to 2 years ahead

        for (var day = 0; day < maxDaysToScan && alternatives.Count < 2; day++)
        {
            var curCheckIn = candidateCheckIn.AddDays(day);
            var curCheckOut = curCheckIn.AddDays(nights);
            var lastNight = curCheckOut.AddDays(-1);

            // check each room's availability and bookable rates for this period
            var availableRoomsForCandidate = new List<(int MaxGuests, decimal MinTotalPrice)>();

            foreach (var r in activeRooms)
            {
                var isBooked = r.Bookings.Any(b => b.CheckIn < curCheckOut && b.CheckOut > curCheckIn);
                if (isBooked)
                    continue;

                // check if the room has at least one rate valid for this period
                var validRates = r.Rates
                    .Where(x => (x.StartDate == null || x.StartDate <= curCheckIn)
                        && (x.EndDate == null || x.EndDate >= lastNight)
                        && x.MinNights <= nights)
                    .ToList();

                if (validRates.Count == 0)
                    continue;

                var lowestRate = validRates.Min(x => x.PricePerNight);
                var pricePerNightIncVat = PriceCalculator.InclusiveOfVat(lowestRate, settings.IsVatEnabled, settings.VatPercentage);
                var totalPrice = PriceCalculator.Total(pricePerNightIncVat, nights);

                availableRoomsForCandidate.Add((r.MaxGuests, totalPrice));
            }

            if (availableRoomsForCandidate.Count >= reqRooms)
            {
                // find combinations that meet the adult capacity requirement
                var validCombos = GetCombinations(availableRoomsForCandidate, reqRooms)
                    .Where(combo => combo.Sum(r => r.MaxGuests) >= reqAdults)
                    .ToList();

                if (validCombos.Count > 0)
                {
                    // price from: cheapest combination of reqRooms that satisfies the capacity
                    var startingPrice = validCombos.Min(combo => combo.Sum(r => r.MinTotalPrice));
                    alternatives.Add(new RoomResponse.AlternativeStayPeriodDto
                    {
                        CheckIn = curCheckIn,
                        CheckOut = curCheckOut,
                        Nights = nights,
                        PriceFrom = startingPrice
                    });
                }
            }
        }

        return alternatives;
    }

    /// <summary>
    /// one room for the detail page, NotFound when the id is unknown or the room is deleted
    /// </summary>
    public async Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        var room = await ActiveRooms
            .Where(r => r.Id == roomId)
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
