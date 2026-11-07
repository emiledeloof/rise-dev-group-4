using Rise.Shared.Amenities;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Amenities;

/// <summary>
/// shows an amenity to the guests again, admin only
/// </summary>
public class Activate(IAmenityService amenityService) : EndpointWithoutRequest<Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Post("/api/amenities/{amenityId:int}/activate");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// hands the id of the url to the service
    /// </summary>
    public override Task<Result> ExecuteAsync(CancellationToken ct)
        => amenityService.ActivateAsync(Route<int>("amenityId"), ct);
}
