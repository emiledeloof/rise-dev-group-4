using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;

namespace Rise.Server.Endpoints.RoomTypes;

/// <summary>
/// all room types for the admin page
/// </summary>
public class Index(IRoomTypeService roomTypeService) : EndpointWithoutRequest<Result<RoomTypeResponse.Index>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Get("/api/room-types");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// the service reads the room types
    /// </summary>
    public override Task<Result<RoomTypeResponse.Index>> ExecuteAsync(CancellationToken ct)
        => roomTypeService.GetIndexAsync(ct);
}
