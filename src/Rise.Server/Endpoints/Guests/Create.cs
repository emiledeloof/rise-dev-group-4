using FastEndpoints;
using Rise.Shared.Guests;

namespace Rise.Server.Endpoints.Guests;

/// <summary>
/// FastEndpoints API endpoint for registering a new guest.
/// Discovered and registered automatically by FastEndpoints at application startup.
/// See: https://fast-endpoints.com/
/// </summary>
/// <param name="guestService">The business logic service injected via dependency injection.</param>
public class Create(IGuestService guestService) : Endpoint<GuestRequest.Create, Result<GuestResponse.Create>>
{
    /// <summary>
    /// Configures the HTTP verb, route, and access policies for this endpoint.
    /// </summary>
    public override void Configure()
    {
        Post("/api/guests");
        AllowAnonymous(); // Guest registration is publicly accessible (no authentication required).
    }

    /// <summary>
    /// Executes the endpoint by delegating the request to <see cref="IGuestService.CreateAsync"/>.
    /// FastEndpoints automatically validates the request against <see cref="GuestRequest.Create.Validator"/>
    /// before this method is called.
    /// The returned <see cref="Result{T}"/> is intercepted by GlobalResponseSender and mapped to HTTP status codes.
    /// </summary>
    public override Task<Result<GuestResponse.Create>> ExecuteAsync(GuestRequest.Create req, CancellationToken ctx)
    {
        return guestService.CreateAsync(req, ctx);
    }
}
