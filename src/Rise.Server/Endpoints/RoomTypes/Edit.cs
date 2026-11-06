using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;

namespace Rise.Server.Endpoints.RoomTypes;

/// <summary>
/// changes a room type, admin only
/// </summary>
public class Edit(IRoomTypeService roomTypeService) : Endpoint<RoomTypeRequest.Edit, Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // same style as the rate pricing edit, the id is in the body
        Put("/api/room-types");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// only called with a valid body, the validator already gave a 400 otherwise
    /// </summary>
    public override Task<Result> ExecuteAsync(RoomTypeRequest.Edit req, CancellationToken ct)
        => roomTypeService.EditAsync(req, ct);
}
