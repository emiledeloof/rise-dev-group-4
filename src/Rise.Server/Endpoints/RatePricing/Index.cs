using Rise.Shared.Identity;
using Rise.Shared.RatePricing;

namespace Rise.Server.Endpoints.RatePricing;

/// <summary>
/// list of all rates with their pricing for the admin page, admin only
/// same idea as Endpoints/Invoice/Index.cs
/// </summary>
public class Index(IRatePricingService ratePricingService) : EndpointWithoutRequest<Result<RatePricingResponse.Index>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // http GET on /api/rate-pricing
        Get("/api/rate-pricing");

        // admin only, Roles with AppRoles.Administrator
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// called on each request, no logic here, it only passes the call to the service
    /// </summary>
    public override Task<Result<RatePricingResponse.Index>> ExecuteAsync(CancellationToken ct)
    {
        // return the result of GetIndexAsync of the service, pass ct
        return ratePricingService.GetIndexAsync(ct);
    }
}
