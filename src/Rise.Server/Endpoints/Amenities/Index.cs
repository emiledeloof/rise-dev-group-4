using Rise.Shared.Amenities;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Amenities;

/// <summary>
/// all amenities, active and inactive, for the admin page
/// </summary>
public class Index(IAmenityService amenityService) : EndpointWithoutRequest<Result<AmenityResponse.Index>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Get("/api/amenities");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// asks the service for the list
    /// </summary>
    public override Task<Result<AmenityResponse.Index>> ExecuteAsync(CancellationToken ct)
        => amenityService.GetIndexAsync(ct);
}
