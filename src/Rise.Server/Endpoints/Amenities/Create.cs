using Rise.Shared.Amenities;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Amenities;

/// <summary>
/// adds an amenity, admin only
/// </summary>
public class Create(IAmenityService amenityService) : Endpoint<AmenityRequest.Create, Result<AmenityResponse.Create>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Post("/api/amenities");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// only called with a valid body, the validator already gave a 400 otherwise
    /// </summary>
    public override Task<Result<AmenityResponse.Create>> ExecuteAsync(AmenityRequest.Create req, CancellationToken ct)
        => amenityService.CreateAsync(req, ct);
}
