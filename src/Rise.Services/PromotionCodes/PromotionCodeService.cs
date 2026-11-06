using Microsoft.EntityFrameworkCore;
using Rise.Domain.Promotions;
using Rise.Persistence;
using Rise.Shared.PromotionCodes;

namespace Rise.Services.PromotionCodes;

public class PromotionCodeService(ApplicationDbContext dbContext) : IPromotionCodeService
{
    public async Task<Result<PromotionCodeResponse.Create>> CreateAsync(
        PromotionCodeRequest.Create request,
        CancellationToken ctx = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.PromotionCodes
            .AnyAsync(x => !x.IsDeleted && x.Code == code, ctx);

        if (exists)
            return Result.Conflict($"A promotion code named '{code}' already exists.");

        var promotionCode = new PromotionCode(
            code,
            request.DiscountType switch
            {
                PromotionCodeDiscountType.Percentage => PromotionDiscountType.Percentage,
                PromotionCodeDiscountType.Fixed => PromotionDiscountType.Fixed,
                _ => throw new ArgumentOutOfRangeException(nameof(request.DiscountType)),
            },
            request.Value,
            request.ValidFrom,
            request.ValidTo,
            string.IsNullOrWhiteSpace(request.EmailRestriction)
                ? null
                : request.EmailRestriction.Trim(),
            request.AppliesTo switch
            {
                PromotionCodeAppliesTo.NonRefundable => PromotionAppliesTo.NonRefundable,
                PromotionCodeAppliesTo.AllRates => PromotionAppliesTo.AllRates,
                _ => throw new ArgumentOutOfRangeException(nameof(request.AppliesTo)),
            })
        {
            IsActive = request.IsActive,
        };

        dbContext.PromotionCodes.Add(promotionCode);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Promotion code {PromotionCodeId} created", promotionCode.Id);

        return Result.Success(new PromotionCodeResponse.Create
        {
            Id = promotionCode.Id,
            Code = promotionCode.Code,
        });
    }
}
