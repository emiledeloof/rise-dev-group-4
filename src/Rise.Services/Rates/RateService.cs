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
            request.Description);

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
            Description = rate.Description,
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
            _ => query.OrderByDescending(rate => rate.Id),
        };

        var activeRates = dbContext.Rates
            .Where(rate => !rate.IsDeleted && !rate.Room.IsDeleted);

        var roomNames = await activeRates
            .Select(rate => rate.Room.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(ctx);

        // only the policies that exist, as the texts the page sends back in the filter
        var usedPolicies = await activeRates
            .Select(rate => rate.CancellationPolicy)
            .Distinct()
            .ToListAsync(ctx);
        var policies = usedPolicies
            .Select(policy => policy == CancellationPolicy.Refundable ? RatePolicies.Refundable : RatePolicies.NonRefundable)
            .OrderBy(policy => policy)
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
                Description = rate.Description,
                // what the edit form needs, same mapping as RoomService
                MinNights = rate.MinNights,
                IsRefundable = rate.CancellationPolicy == CancellationPolicy.Refundable,
                FullRefundDays = rate.FullRefundDays,
                PartialRefundDays = rate.PartialRefundDays,
                PartialRefundPercent = rate.PartialRefundPercent,
            })
            .ToListAsync(ctx);

        return Result.Success(new RateResponse.Index
        {
            Rates = rates,
            RoomNames = roomNames,
            Policies = policies,
            TotalCount = totalCount,
        });
    }
}