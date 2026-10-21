using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.RatePricing;

namespace Rise.Services.RatePricing;

/// <summary>
/// pricing of the rates for the admin page, reads and saves the rates in the db
/// same idea as RoomService
/// </summary>
public class RatePricingService(ApplicationDbContext dbContext) : IRatePricingService
{
    /// <summary>
    /// all rates not deleted with the name of their room, eg Deluxe Room, Standard, 175
    /// </summary>
    public async Task<Result<RatePricingResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        // start from dbContext.Rates, keep only the ones not deleted
        var rates = await dbContext.Rates
            .Where(r => !r.IsDeleted)
            // read only, nothing is changed here so AsNoTracking, faster
            .AsNoTracking()
            // sort by room name then rate name, OrderBy then ThenBy, so the admin always sees the same order
            .OrderBy(r => r.Room.Name)
            .ThenBy(r => r.Name)
            // turn each rate into a RatePricingDto.Index with Select, the room name comes from r.Room.Name
            .Select(r => new RatePricingDto.Index
            {
                RateId = r.Id,
                RoomName = r.Room.Name,
                RateName = r.Name,
                PricePerNight = r.PricePerNight
            })
            // run the query with ToListAsync(ctx), await it
            .ToListAsync(ctx);

        // wrap the list in a RatePricingResponse.Index and return Result.Success(...)
        return Result.Success(new RatePricingResponse.Index { Rates = rates });
    }

    /// <summary>
    /// saves the new price of one rate, NotFound if the rate does not exist
    /// </summary>
    public async Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default)
    {
        // find the rate with the id of the request and not deleted, FirstOrDefaultAsync
        // no AsNoTracking here, EF must follow the rate to save the change
        var rate = await dbContext.Rates
            .FirstOrDefaultAsync(r => r.Id == request.RateId && !r.IsDeleted, ctx);

        // rate not found, write a warning in the logs with the id, Log.Warning(...)
        // then return Result.NotFound with a clear msg, see BookingService for the style
        if (rate is null)
        {
            Log.Warning("Rate {RateId} not found", request.RateId);
            return Result.NotFound($"Rate with Id '{request.RateId}' was not found.");
        }

        // set the new price, the setter in Rate refuses 0 or less
        rate.PricePerNight = request.PricePerNight;

        // save the change in the db, await dbContext.SaveChangesAsync(ctx)
        await dbContext.SaveChangesAsync(ctx);

        // write the change in the logs, rate id and new price, Log.Information(...)
        Log.Information("Rate {RateId} price changed to {Price}", request.RateId, request.PricePerNight);

        return Result.Success();
    }
}
