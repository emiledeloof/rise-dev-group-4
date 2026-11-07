using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;

namespace Rise.Server.Endpoints.RoomTypes;

/// <summary>
/// one room type with its amenities and bed configurations for the edit form, admin only
/// </summary>
public class Detail(IRoomTypeService roomTypeService) : EndpointWithoutRequest<Result<RoomTypeDto.Edit>>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        Get("/api/room-types/{roomTypeId:int}");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// hands the id of the url to the service
    /// </summary>
    public override Task<Result<RoomTypeDto.Edit>> ExecuteAsync(CancellationToken ct)
        => roomTypeService.GetForEditAsync(Route<int>("roomTypeId"), ct);
}
