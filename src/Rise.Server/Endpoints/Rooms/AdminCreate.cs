using Rise.Shared.Identity;
using Rise.Shared.Rooms;

namespace Rise.Server.Endpoints.Rooms;

/// <summary>adds a physical room, admin only</summary>
public class AdminCreate(IRoomAdminService roomAdminService) : Endpoint<RoomRequest.Create, Result<int>>
{
    /// <summary>route and access rights of the endpoint</summary>
    public override void Configure()
    {
        Post("/api/admin/rooms");
        Roles(AppRoles.Administrator);
    }

    /// <summary>only called with a valid body, the validator already gave a 400 otherwise</summary>
    public override Task<Result<int>> ExecuteAsync(RoomRequest.Create req, CancellationToken ct)
        => roomAdminService.CreateAsync(req, ct);
}
