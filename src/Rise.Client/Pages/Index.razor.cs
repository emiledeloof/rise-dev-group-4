using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;

namespace Rise.Client.Pages;

/// <summary>
/// landing page, the rooms come from the api, the hero, estate and reviews are static
/// </summary>
public partial class Index
{
    // photos of the hero slideshow, in order, the css keyframes are made for 5 photos of 6 s each
    private const int SlideSeconds = 6;

    // max nb of room cards, one row of the card grid on big screens
    private const int FeaturedRoomsCount = 3;

    private static readonly string[] HeroPhotos =
    [
        "images/home/hero-1-house.jpg",
        "images/home/hero-2-room.jpg",
        "images/home/hero-3-sunset.jpg",
        "images/home/hero-4-beach.jpg",
        "images/home/hero-5-coast.jpg",
    ];

    // photos of the estate section: the house, the garden, the breakfast terrace, a vineyard and the statue of the cave
    private static readonly string[] EstatePhotos =
    [
        "images/home/estate.jpg",
        "images/home/estate-2.jpg",
        "images/home/estate-3.jpg",
        "images/home/estate-4.jpg",
        "images/home/estate-5.jpg",
    ];

    private record ReviewItem(string Text, string Author, string Source);

    // TODO: load the reviews from google once a story asks for it
    private static readonly ReviewItem[] ReviewList =
    [
        new("A quiet, beautiful place among the vines. We will be back.", "Anna", "Google Reviews"),
        new("Warm hosts, stunning breakfast on the terrace.", "Marc", "Google Reviews"),
        new("Perfect base for exploring the coast.", "Sofia", "Facebook"),
    ];

    // client RoomService registered in Program.cs, calls the room list endpoint
    [Inject] public required IRoomService RoomService { get; set; }

    // rooms of the cards, empty while loading or when they could not be loaded
    private IReadOnlyList<RoomDto.Summary> _rooms = [];

    // nb of rooms in the db, more than the cards means a link to see all rooms
    private int _totalCount;
    private int _totalCapacity;

    // true while the api is still answering
    private bool _isLoading = true;

    // msg shown instead of the cards when there is no room to show
    private string? _errorMessage;

    // more rooms in the db than there are cards
    private bool HasMoreRooms => _totalCount > _rooms.Count;

    /// <summary>
    /// runs when the page opens, loads the first rooms
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var result = await RoomService.GetListAsync(new RoomRequest.List { Take = FeaturedRoomsCount });

        if (result.IsSuccess)
        {
            _rooms = result.Value.Rooms.ToList();
            _totalCount = result.Value.TotalCount;
            _totalCapacity = result.Value.TotalCapacity;
        }

        // an empty list gets the same msg as an error, the section never stays blank
        if (_rooms.Count == 0)
            _errorMessage = "Our rooms could not be loaded right now.";

        _isLoading = false;
    }
}
