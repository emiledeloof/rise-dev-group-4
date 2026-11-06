using Rise.Shared.Identity;
using Rise.Shared.Rooms;

namespace Rise.Server.Endpoints.Rooms;

/// <summary>all physical rooms for the admin manage rooms page</summary>
public class AdminIndex(IRoomAdminService roomAdminService) : EndpointWithoutRequest<Result<IReadOnlyList<RoomAdminDto.Index>>>
{
    /// <summary>route and access rights of the endpoint</summary>
    public override void Configure()
    {
        Get("/api/admin/rooms");
        Roles(AppRoles.Administrator);
    }

    /// <summary>the service reads the rooms</summary>
    public override Task<Result<IReadOnlyList<RoomAdminDto.Index>>> ExecuteAsync(CancellationToken ct)
        => roomAdminService.GetIndexAsync(ct);
}
