using Rise.Shared.Search;

namespace Rise.Shared.Rooms;

/// <summary>
/// contract for the room features, the server implements it and later the client too
/// same idea as IInvoiceService
/// </summary>
public interface IRoomService
{
    // gets the rooms that are free for the given stay
    // takes the stay period from the search, returns a Result with the room list inside
    Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default);

    // gets one room for the room detail page, NotFound when it does not exist or is deleted
    Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default);

    // gets the first rooms for the landing page, at most request.Take, with the total nb of rooms
    Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default);

}
