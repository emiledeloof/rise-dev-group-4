using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;

namespace Rise.Server.Endpoints.RoomTypes;

/// <summary>
/// adds a room type, admin only
/// </summary>
public class Create(IRoomTypeService roomTypeService)
    : Endpoint<RoomTypeRequest.Create, Result<RoomTypeResponse.Create>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Post("/api/room-types");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// only called with a valid body, the validator already gave a 400 otherwise
    /// </summary>
    public override Task<Result<RoomTypeResponse.Create>> ExecuteAsync(RoomTypeRequest.Create req, CancellationToken ct)
        => roomTypeService.CreateAsync(req, ct);
}
