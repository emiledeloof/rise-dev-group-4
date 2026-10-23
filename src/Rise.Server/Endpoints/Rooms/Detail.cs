using Rise.Shared.Rooms;

namespace Rise.Server.Endpoints.Rooms;

/// <summary>
/// one room, called by the room detail page
/// </summary>
/// <param name="roomService">reads the room from the db</param>
public class Detail(IRoomService roomService) : EndpointWithoutRequest<Result<RoomDto.Detail>>
{
    /// <summary>
    /// url, http verb and who can call it
    /// </summary>
    public override void Configure()
    {
        // id of the room in the url eg /api/rooms/3
        Get("/api/rooms/{roomId:int}");

        // any visitor can look at a room, no login needed
        AllowAnonymous();
    }

    /// <summary>
    /// hands the id of the url to the service
    /// </summary>
    public override Task<Result<RoomDto.Detail>> ExecuteAsync(CancellationToken ct)
    {
        var roomId = Route<int>("roomId");
        return roomService.GetDetailAsync(roomId, ct);
    }
}
