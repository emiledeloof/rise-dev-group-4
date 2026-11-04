using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;

namespace Rise.Server.Endpoints.RoomTypes;

/// <summary>
/// deletes a room type that no room uses, admin only
/// </summary>
public class Delete(IRoomTypeService roomTypeService) : EndpointWithoutRequest<Result>
{
    /// <summary>
    /// route and access rights of the endpoint
    /// </summary>
    public override void Configure()
    {
        // ? verbs and routes instead of the delete helper, the class has the same name
        Verbs(Http.DELETE);
        Routes("/api/room-types/{roomTypeId:int}");
        Roles(AppRoles.Administrator);
    }

    /// <summary>
    /// hands the id of the url to the service
    /// </summary>
    public override Task<Result> ExecuteAsync(CancellationToken ct)
        => roomTypeService.DeleteAsync(Route<int>("roomTypeId"), ct);
}
