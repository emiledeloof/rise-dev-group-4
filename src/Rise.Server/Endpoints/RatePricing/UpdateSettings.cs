using Rise.Shared.Identity;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;

namespace Rise.Server.Endpoints.RatePricing;

public class UpdateSettings(IRatePricingService ratePricingService)
    : Endpoint<GeneralSettingsRequest.Update, Result<GeneralSettingsDto>>
{
    public override void Configure()
    {
        Put("/api/rate-pricing/settings");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<GeneralSettingsDto>> ExecuteAsync(
        GeneralSettingsRequest.Update request,
        CancellationToken ct)
        => ratePricingService.UpdateGeneralSettingsAsync(request, ct);
}
