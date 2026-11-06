using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Client.I18nText;
using Rise.Shared.Rooms;

namespace Rise.Client.Rooms;

/// <summary>
/// overview of all the rooms, a showcase of the first ones then a list of every room with a link to its detail page
/// </summary>
public partial class Overview : LocalizedComponentBase
{

    // max nb of rooms asked to the api, the max of the list validator
    // TODO: paging once the hotel has more than 20 rooms
    private const int MaxRooms = 20;

    // nb of rooms shown in big blocks, the others are only in the list
    private const int MaxFeaturedRooms = 3;

    // nb of photos of the mosaic, the css grid is made for 5
    private const int MaxMosaicPhotos = 5;

    private record ExperienceItem(string Icon, string Title, string Text);

    // what every guest gets, same facts as the estate section of the landing page
    private static readonly ExperienceItem[] ExperienceList =
    [
        new("fa-seedling", "Gardens & vineyards", "2.4 hectares of gardens and two small vineyards right outside your door."),
        new("fa-mountain", "Granite wood", "A wood with granite rocks and caves, where the statue of Mary was found."),
        new("fa-mug-hot", "Breakfast on the terrace", "Start the day slowly on the terrace, among the vines."),
        new("fa-umbrella-beach", "Close to the coast", "The beaches and the coast are a short drive away."),
    ];

    /// <summary>
    /// one photo of the mosaic with the room it belongs to
    /// </summary>
    private record MosaicPhoto(string Url, int RoomId, string RoomName);

    // client RoomService registered in Program.cs, calls the room list endpoint
    [Inject] public required IRoomService RoomService { get; set; }

    // rooms of the page, empty while loading or when they could not be loaded
    private IReadOnlyList<RoomDto.Summary> _rooms = [];

    // true while the api is still answering
    private bool _isLoading = true;

    // msg shown instead of the rooms when there is no room to show
    private string? _errorMessage;

    // first rooms, shown in big blocks
    private IReadOnlyList<RoomDto.Summary> FeaturedRooms => _rooms.Take(MaxFeaturedRooms).ToList();

    // photos of the mosaic, one photo of each room in turn so every room is shown before a 2nd photo of the same room
    private IReadOnlyList<MosaicPhoto> MosaicPhotos =>
        _rooms
            .SelectMany(r => r.PhotoUrls.Select((url, index) => (Photo: new MosaicPhoto(url, r.Id, r.Name), Index: index)))
            .OrderBy(x => x.Index)
            .Select(x => x.Photo)
            .Take(MaxMosaicPhotos)
            .ToList();

    /// <summary>
    /// runs when the page opens, loads all the rooms
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadTextAsync();

        var result = await RoomService.GetListAsync(new RoomRequest.List { Take = MaxRooms });

        if (!result.IsSuccess)
        {
            _errorMessage = "Our rooms could not be loaded right now.";
        }
        else
        {
            _rooms = result.Value.Rooms.ToList();

            // no room in the db yet, a msg so the page never stays blank
            if (_rooms.Count == 0)
                _errorMessage = "Our rooms are being prepared, please come back soon.";
        }

        _isLoading = false;
    }
}
