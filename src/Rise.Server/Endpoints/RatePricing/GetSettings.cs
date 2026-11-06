using Rise.Shared.Identity;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;

namespace Rise.Server.Endpoints.RatePricing;

public class GetSettings(IRatePricingService ratePricingService)
    : EndpointWithoutRequest<Result<GeneralSettingsDto>>
{
    public override void Configure()
    {
        Get("/api/rate-pricing/settings");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<GeneralSettingsDto>> ExecuteAsync(CancellationToken ct)
        => ratePricingService.GetGeneralSettingsAsync(ct);
}
