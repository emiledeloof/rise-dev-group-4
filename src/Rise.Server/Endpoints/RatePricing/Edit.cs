using Rise.Shared.Identity;
using Rise.Shared.RatePricing;

namespace Rise.Server.Endpoints.RatePricing;

/// <summary>
/// saves the new price of one rate, admin only
/// the Edit validator runs automatically before ExecuteAsync, an invalid price gives a 400
/// </summary>
public class Edit(IRatePricingService ratePricingService) : Endpoint<RatePricingRequest.Edit, Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // http PUT on /api/rate-pricing, put = change something that already exists
        Put("/api/rate-pricing");

        // admin only, Roles with AppRoles.Administrator
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// called on each request with a valid body, only passes the call to the service
    /// </summary>
    public override Task<Result> ExecuteAsync(RatePricingRequest.Edit req, CancellationToken ct)
    {
        // return the result of EditAsync of the service, pass req and ct
        return ratePricingService.EditAsync(req, ct);
    }
}
