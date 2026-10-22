using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
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
    /// all rates not deleted with the name of their room, sorted by room then rate
    /// </summary>
    public async Task<Result<RatePricingResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var rates = await dbContext.Rates
            .Where(r => !r.IsDeleted)
            // * read only, no tracking needed, faster
            .AsNoTracking()
            // same order every time for the admin
            .OrderBy(r => r.Room.Name)
            .ThenBy(r => r.Name)
            // only the fields the page needs, the room name comes from the join
            .Select(r => new RatePricingDto.Index
            {
                RateId = r.Id,
                RoomName = r.Room.Name,
                RateName = r.Name,
                PricePerNight = r.PricePerNight,
                MinNights = r.MinNights,
                // * shared cant see the enum, so the page gets a bool
                IsRefundable = r.CancellationPolicy == CancellationPolicy.Refundable,
                FullRefundDays = r.FullRefundDays,
                PartialRefundDays = r.PartialRefundDays,
                PartialRefundPercent = r.PartialRefundPercent
            })
            // ! the query only runs here
            .ToListAsync(ctx);

        return Result.Success(new RatePricingResponse.Index { Rates = rates });
    }

    /// <summary>
    /// saves the pricing of one rate, NotFound if the rate does not exist
    /// </summary>
    public async Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default)
    {
        // ! tracked on purpose, EF must follow the rate to save the changes
        var rate = await dbContext.Rates
            .FirstOrDefaultAsync(r => r.Id == request.RateId && !r.IsDeleted, ctx);

        if (rate is null)
        {
            Log.Warning("Rate {RateId} not found", request.RateId);
            return Result.NotFound($"Rate with Id '{request.RateId}' was not found.");
        }

        // * the setters in Rate refuse invalid values, last safety net after the validator
        rate.PricePerNight = request.PricePerNight;
        rate.MinNights = request.MinNights;

        // * bool from the page back to the domain enum
        var newPolicy = request.IsRefundable ? CancellationPolicy.Refundable : CancellationPolicy.NonRefundable;

        // ! before the policy is set, the old one is needed to know if it changed
        RenameAfterPolicy(rate, newPolicy);
        rate.CancellationPolicy = newPolicy;

        // ! only for a refundable rate, the validator skips these values otherwise
        // so a bad value here would hit the Guard and give a 500, a non refundable rate keeps its old values
        if (request.IsRefundable)
        {
            rate.FullRefundDays = request.FullRefundDays;
            rate.PartialRefundDays = request.PartialRefundDays;
            rate.PartialRefundPercent = request.PartialRefundPercent;
        }

        await dbContext.SaveChangesAsync(ctx);

        // what is saved now, useful to debug
        Log.Information("Rate {RateId} updated, price {Price}, min nights {MinNights}, refundable {IsRefundable}", request.RateId, request.PricePerNight, request.MinNights, request.IsRefundable);

        return Result.Success();
    }

    // default name and conditions of a non refundable rate, eg from the seed
    private const string NonRefundableName = "Non-refundable";
    private const string NonRefundableConditions = "No refund after booking";

    // default name of a refundable rate
    private const string RefundableName = "Refundable";

    /// <summary>
    /// a rate named after its policy follows it, eg Non-refundable becomes Refundable when the box is checked
    /// a custom name, eg Breakfast included, is never changed
    /// </summary>
    private static void RenameAfterPolicy(Rate rate, CancellationPolicy newPolicy)
    {
        // same policy, nothing to rename
        if (newPolicy == rate.CancellationPolicy)
            return;

        if (newPolicy == CancellationPolicy.Refundable && rate.Name == NonRefundableName)
        {
            rate.Name = RefundableName;
            // * no refund after booking is wrong now, the guest sees the refund rules instead
            if (rate.Description == NonRefundableConditions)
                rate.Description = null;
        }
        else if (newPolicy == CancellationPolicy.NonRefundable && rate.Name == RefundableName)
        {
            rate.Name = NonRefundableName;
            rate.Description ??= NonRefundableConditions;
        }
    }
}
