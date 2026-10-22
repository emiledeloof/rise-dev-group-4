using Rise.Shared.Identity;
using Rise.Shared.RatePricing;

namespace Rise.Server.Endpoints.RatePricing;

/// <summary>
/// saves the pricing of one rate, admin only
/// </summary>
public class Edit(IRatePricingService ratePricingService) : Endpoint<RatePricingRequest.Edit, Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // put = change something that already exists
        Put("/api/rate-pricing");

        // ! real security, the [Authorize] on the page can be bypassed
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// only called with a valid body, the Edit validator already ran and gave a 400 otherwise
    /// </summary>
    public override Task<Result> ExecuteAsync(RatePricingRequest.Edit req, CancellationToken ct)
    {
        // * no logic here, the service does the work
        return ratePricingService.EditAsync(req, ct);
    }
}
