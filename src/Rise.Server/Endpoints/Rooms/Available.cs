using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Server.Endpoints.Rooms;

/// <summary>
/// rooms free for a stay, called by the search results page
/// same idea as the Invoice endpoint
/// </summary>
/// <param name="roomService">does the real search in the db</param>
public class Available(IRoomService roomService) : Endpoint<SearchRequest.StayPeriod, Result<RoomResponse.Available>>
{
    /// <summary>
    /// url, http verb and who can call it
    /// </summary>
    public override void Configure()
    {
        // GET because we only read, the dates come in the url eg ?checkIn=2026-10-10&checkOut=2026-10-13
        Get("/api/rooms/available");

        // any visitor can search, no login needed
        AllowAnonymous();
    }

    /// <summary>
    /// runs after the StayPeriod validator said the dates are ok
    /// </summary>
    public override Task<Result<RoomResponse.Available>> ExecuteAsync(SearchRequest.StayPeriod req, CancellationToken ct)
    {
        // just hand the dates to the service
        return roomService.GetAvailableAsync(req, ct);
    }
}
