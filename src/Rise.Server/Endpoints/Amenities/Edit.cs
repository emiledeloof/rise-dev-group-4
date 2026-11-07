using Rise.Shared.Amenities;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Amenities;

/// <summary>
/// changes the names and icon of an amenity, admin only
/// </summary>
public class Edit(IAmenityService amenityService) : Endpoint<AmenityRequest.Edit, Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // same style as the room type edit, the id is in the body
        Put("/api/amenities");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// only called with a valid body, the validator already gave a 400 otherwise
    /// </summary>
    public override Task<Result> ExecuteAsync(AmenityRequest.Edit req, CancellationToken ct)
        => amenityService.EditAsync(req, ct);
}
