using Rise.Shared.Identity;
using Rise.Shared.PromotionCodes;

namespace Rise.Server.Endpoints.PromotionCodes;

public class Create(IPromotionCodeService promotionCodeService)
    : Endpoint<PromotionCodeRequest.Create, Result<PromotionCodeResponse.Create>>
{
    public override void Configure()
    {
        Post("/api/promotion-codes");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<PromotionCodeResponse.Create>> ExecuteAsync(
        PromotionCodeRequest.Create req,
        CancellationToken ct)
        => promotionCodeService.CreateAsync(req, ct);
}
