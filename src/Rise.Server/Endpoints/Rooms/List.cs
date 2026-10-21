using Rise.Shared.Rooms;

namespace Rise.Server.Endpoints.Rooms;

/// <summary>
/// the first rooms, called by the landing page
/// same idea as the Available endpoint
/// </summary>
/// <param name="roomService">reads the rooms from the db</param>
public class List(IRoomService roomService) : Endpoint<RoomRequest.List, Result<RoomResponse.List>>
{
    /// <summary>
    /// url, http verb and who can call it
    /// </summary>
    public override void Configure()
    {
        // GET because we only read, the max nb of rooms comes in the url eg ?take=3
        Get("/api/rooms");

        // any visitor can see the rooms, no login needed
        AllowAnonymous();
    }

    /// <summary>
    /// runs after the List validator said take is ok
    /// </summary>
    public override Task<Result<RoomResponse.List>> ExecuteAsync(RoomRequest.List req, CancellationToken ct)
    {
        // just hand the request to the service
        return roomService.GetListAsync(req, ct);
    }
}
