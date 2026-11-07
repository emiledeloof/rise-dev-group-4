using Rise.Shared.Amenities;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Amenities;

/// <summary>
/// hides an amenity from the guests, it stays assigned to its room types, admin only
/// </summary>
public class Deactivate(IAmenityService amenityService) : EndpointWithoutRequest<Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Post("/api/amenities/{amenityId:int}/deactivate");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// hands the id of the url to the service
    /// </summary>
    public override Task<Result> ExecuteAsync(CancellationToken ct)
        => amenityService.DeactivateAsync(Route<int>("amenityId"), ct);
}
