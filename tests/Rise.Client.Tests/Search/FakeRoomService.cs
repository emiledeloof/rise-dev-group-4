using Ardalis.Result;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Client.Search;

/// <summary>
/// fake IRoomService for the page tests, no http call
/// each test chooses what the fake sends back
/// </summary>
public class FakeRoomService : IRoomService
{
    /// <summary>
    /// what GetAvailableAsync sends back, 2 rooms by default
    /// </summary>
    public Result<RoomResponse.Available> Answer { get; set; } = Result.Success(new RoomResponse.Available
    {
        Rooms =
        [
            new RoomDto.Available { Id = 1, Name = "Double Room", MaxGuests = 2, PricePerNight = 109m, Nights = 3, TotalPrice = 327m },
            new RoomDto.Available { Id = 2, Name = "Single Room", MaxGuests = 1, PricePerNight = 79m, Nights = 3, TotalPrice = 237m },
        ],
    });

    /// <summary>
    /// every stay the page asked for, to check the dates it sent
    /// </summary>
    public List<SearchRequest.StayPeriod> Requests { get; } = [];

    /// <summary>
    /// remembers the stay and sends back the chosen answer
    /// </summary>
    public Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        Requests.Add(request);
        return Task.FromResult(Answer);
    }
}
