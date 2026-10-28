using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Shared.Common;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;

namespace Rise.Services.Rates;

public sealed class RateService(ApplicationDbContext dbContext) : IRateService
{
    public async Task<Result<RateResponse.Create>> CreateAsync(
        RateRequest.Create request,
        CancellationToken cancellationToken = default)
    {
        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Name == request.RoomName && !r.IsDeleted, cancellationToken);

        if (room is null)
        {
            return Result.NotFound("Room not found.");
        }

        var rate = room.AddRate(
            request.Name,
            request.PricePerNight,
            request.Description,
            request.StartDate,
            request.EndDate);

        rate.IsActive = request.IsActive;

        // * policy chosen in the form, a new rate is refundable in the domain so it is always set here
        rate.MinNights = request.MinNights;
        rate.CancellationPolicy = request.IsRefundable ? CancellationPolicy.Refundable : CancellationPolicy.NonRefundable;

        // ! refund values only for a refundable rate, the validator skips them otherwise
        if (request.IsRefundable)
        {
            rate.FullRefundDays = request.FullRefundDays;
            rate.PartialRefundDays = request.PartialRefundDays;
            rate.PartialRefundPercent = request.PartialRefundPercent;
        }

        dbContext.Rates.Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RateResponse.Create
        {
            Id = rate.Id,
            Name = rate.Name,
            RoomName = rate.Room.Name,
            PricePerNight = rate.PricePerNight,
            IsActive = rate.IsActive,
            Description = rate.Description,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate
        });
    }

    public async Task<Result<RateResponse.Index>> GetIndexAsync(
        RateRequest.Index request,
        CancellationToken ctx = default)
    {

        var query = dbContext.Rates
            .Where(rate => !rate.IsDeleted && !rate.Room.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(rate =>
                rate.Name.Contains(searchTerm) ||
                rate.Room.Name.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.RoomName))
            query = query.Where(rate => rate.Room.Name == request.RoomName);

        // * filter on the real cancellation policy, not on the description text
        if (request.Policy == RatePolicies.Refundable)
            query = query.Where(rate => rate.CancellationPolicy == CancellationPolicy.Refundable);
        else if (request.Policy == RatePolicies.NonRefundable)
            query = query.Where(rate => rate.CancellationPolicy == CancellationPolicy.NonRefundable);

        if (request.FilterByDateRange)
        {
            query = query.Where(rate =>
                rate.StartDate == request.StartDate &&
                rate.EndDate == request.EndDate);
        }
        else
        {
            if (request.StartDate is not null)
                query = query.Where(rate => rate.StartDate == request.StartDate);

            if (request.EndDate is not null)
                query = query.Where(rate => rate.EndDate == request.EndDate);
        }

        var totalCount = await query.CountAsync(ctx);

        query = request.OrderBy switch
        {
            "Name" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.Name)
                : query.OrderBy(rate => rate.Name),
            "RoomName" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.Room.Name)
                : query.OrderBy(rate => rate.Room.Name),
            "PricePerNight" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.PricePerNight)
                : query.OrderBy(rate => rate.PricePerNight),
            "StartDate" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.StartDate)
                : query.OrderBy(rate => rate.StartDate),
            "EndDate" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.EndDate)
                : query.OrderBy(rate => rate.EndDate),
            _ => query.OrderByDescending(rate => rate.Id),
        };

        var allNonDeletedRates = dbContext.Rates
            .Where(rate => !rate.IsDeleted && !rate.Room.IsDeleted);

        var roomNames = await allNonDeletedRates
            .Select(rate => rate.Room.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(ctx);

        // only the policies that exist, as the texts the page sends back in the filter
        var usedPolicies = await allNonDeletedRates
            .Select(rate => rate.CancellationPolicy)
            .Distinct()
            .ToListAsync(ctx);
        var policies = usedPolicies
            .Select(policy => policy == CancellationPolicy.Refundable ? RatePolicies.Refundable : RatePolicies.NonRefundable)
            .OrderBy(policy => policy)
            .ToList();

        var dateRanges = await allNonDeletedRates
            .Select(rate => new { rate.StartDate, rate.EndDate })
            .Distinct()
            .ToListAsync(ctx);
        var dateRangeOptions = dateRanges
            .Where(period => period.StartDate is not null || period.EndDate is not null)
            .Select(period =>
                $"{period.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""} - " +
                $"{period.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""}")
            .OrderBy(period => period)
            .ToList();

        var rates = await query
            .AsNoTracking()
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(rate => new RateDto.Index
            {
                Id = rate.Id,
                Name = rate.Name,
                RoomName = rate.Room.Name,
                PricePerNight = rate.PricePerNight,
                IsActive = rate.IsActive,
                Description = rate.Description,
                // what the edit form needs, same mapping as RoomService
                MinNights = rate.MinNights,
                IsRefundable = rate.CancellationPolicy == CancellationPolicy.Refundable,
                FullRefundDays = rate.FullRefundDays,
                PartialRefundDays = rate.PartialRefundDays,
                PartialRefundPercent = rate.PartialRefundPercent,
                StartDate = rate.StartDate,
                EndDate = rate.EndDate,
            })
            .ToListAsync(ctx);

        return Result.Success(new RateResponse.Index
        {
            Rates = rates,
            RoomNames = roomNames,
            Policies = policies,
            DateRanges = dateRangeOptions,
            TotalCount = totalCount,
        });
    }
}