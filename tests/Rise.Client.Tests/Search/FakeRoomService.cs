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
            new RoomDto.Available
            {
                Id = 1, Name = "Double Room", MaxGuests = 2, PricePerNight = 109m, Nights = 3, TotalPrice = 327m,
                Rates =
                [
                    new RateDto.Available
                    {
                        Id = 1, Name = "Non-refundable", PricePerNight = 109m, TotalPrice = 327m,
                        IsRefundable = false, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50,
                    },
                    new RateDto.Available
                    {
                        Id = 2, Name = "Fully refundable", PricePerNight = 139m, TotalPrice = 417m,
                        IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50,
                    },
                ],
            },
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

    /// <summary>
    /// what GetDetailAsync sends back, a double room with a description, characteristics and amenities by default
    /// </summary>
    public Result<RoomDto.Detail> DetailAnswer { get; set; } = Result.Success(new RoomDto.Detail
    {
        Id = 1,
        Name = "Double Room",
        Description = "Spacious room with a double bed.",
        MaxGuests = 2,
        PhotoUrls = ["images/rooms/double-1.jpg", "images/rooms/double-2.jpg"],
        SizeInSquareMeters = 25,
        BedType = "Double bed",
        View = "Garden view",
        Policies = "A baby cot is available on request.",
        Amenities =
        [
            new AmenityDto.Index { Name = "Hairdryer", Icon = "fa-wind" },
            new AmenityDto.Index { Name = "Wi-Fi", Icon = "fa-wifi" },
        ],
    });

    /// <summary>
    /// sends back the chosen detail answer
    /// </summary>
    public Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        return Task.FromResult(DetailAnswer);
    }

    /// <summary>
    /// what GetListAsync sends back, the 3 rooms of the seed by default and no other room
    /// </summary>
    public Result<RoomResponse.List> ListAnswer { get; set; } = Result.Success(new RoomResponse.List
    {
        Rooms =
        [
            new RoomDto.Summary
            {
                Id = 1, Name = "Standard Room", Description = "Cosy room with a view on the vines.", MaxGuests = 2, FromPrice = 133m,
                PhotoUrls = ["images/rooms/standard-room-1.jpg", "images/rooms/standard-room-2.jpg"],
            },
            new RoomDto.Summary
            {
                Id = 2, Name = "Deluxe Room", Description = "Spacious room with a terrace.", MaxGuests = 2, FromPrice = 175m,
                PhotoUrls = ["images/rooms/deluxe-room-1.jpg"],
            },
            new RoomDto.Summary
            {
                Id = 3, Name = "Deluxe Room with bathtub", Description = "Spacious room with a bathtub.", MaxGuests = 2, FromPrice = 175m,
                PhotoUrls = ["images/rooms/deluxe-room-bathtub-1.jpg"],
            },
        ],
        TotalCount = 3,
    });

    /// <summary>
    /// every list request the page sent, to check how many rooms it asked for
    /// </summary>
    public List<RoomRequest.List> ListRequests { get; } = [];

    /// <summary>
    /// remembers the request and sends back the chosen list answer
    /// </summary>
    public Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default)
    {
        ListRequests.Add(request);
        return Task.FromResult(ListAnswer);
    }
}
