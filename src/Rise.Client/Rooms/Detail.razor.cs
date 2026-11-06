using System.Globalization;
using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Client.I18nText;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Client.Rooms;

/// <summary>
/// room detail page, shows what the room offers, opened from the search results
/// </summary>
public partial class Detail : LocalizedComponentBase
{

    /// <summary>
    /// id of the room from the url, eg /rooms/3
    /// </summary>
    [Parameter] public int RoomId { get; set; }

    /// <summary>
    /// dates of the search the guest came from, only used for the back btn, eg ?checkIn=2026-10-14
    /// </summary>
    [SupplyParameterFromQuery] private DateOnly? CheckIn { get; set; }

    /// <summary>
    /// departure date of the search the guest came from, eg checkOut=2026-10-17
    /// </summary>
    [SupplyParameterFromQuery] private DateOnly? CheckOut { get; set; }

    /// <summary>
    /// guest count of the search the guest came from, eg adults=2
    /// </summary>
    [SupplyParameterFromQuery] private int? Adults { get; set; }

    /// <summary>
    /// room count of the search the guest came from, eg rooms=1
    /// </summary>
    [SupplyParameterFromQuery] private int? Rooms { get; set; }

    // client RoomService registered in Program.cs, calls the room detail endpoint
    [Inject] public required IRoomService RoomService { get; set; }

    // room from the api, null while loading or when it could not be loaded
    private RoomDto.Detail? _room;

    // true while the api is still answering
    private bool _isLoading = true;

    // msg shown when there is no room to show
    private string? _errorMessage;

    // came from the results with dates, so go back to the same results, otherwise to the search form
    private bool HasSearchDates => CheckIn is not null && CheckOut is not null;
    private string BackUrl => HasSearchDates
        ? $"search/results?checkIn={CheckIn:yyyy-MM-dd}&checkOut={CheckOut:yyyy-MM-dd}{(Adults.HasValue ? $"&adults={Adults}" : "")}{(Rooms.HasValue ? $"&rooms={Rooms}" : "")}"
        : "search";
    private string BackText => HasSearchDates ? Text["BackToAvailableRooms"] : Text["SearchRooms"];

    // dates of the url grouped in a stay period, to reuse its Nights calculation
    private SearchRequest.StayPeriod Stay => new()
    {
        CheckIn = CheckIn,
        CheckOut = CheckOut,
        Adults = Adults ?? SearchRequest.StayPeriod.DefaultAdults,
        Rooms = Rooms ?? SearchRequest.StayPeriod.DefaultRooms,
    };

    // dates of the stay card, eg 14 Oct – 17 Oct 2026
    private string StayDatesText =>
        $"{CheckIn?.ToString("d MMM", DateDisplayCulture)} – {CheckOut?.ToString("d MMM yyyy", DateDisplayCulture)}";

    private static CultureInfo DateDisplayCulture =>
        OperatingSystem.IsBrowser() ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;

    // main btn of the stay card, same target as the back link, with a text that invites to book
    private string CtaUrl => BackUrl;
    private string CtaText => HasSearchDates ? Text["SeePricesForDates"] : Text["CheckAvailability"];

    // 1st photo is the static hero, null when the room has no photo
    private string? HeroPhoto => _room?.PhotoUrls.FirstOrDefault();

    // at least 1 characteristic filled in, otherwise the empty state is shown
    private bool HasCharacteristics =>
        _room is not null && (_room.SizeInSquareMeters is not null || _room.BedType is not null || _room.View is not null);

    // the other photos go in the slider of the gallery
    private IReadOnlyList<string> GalleryPhotos => _room?.PhotoUrls.Skip(1).ToList() ?? [];

    /// <summary>
    /// runs when the page opens and each time the id in the url changes, loads the room
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _room = null;
        _errorMessage = null;

        var result = await RoomService.GetDetailAsync(RoomId);

        if (result.IsSuccess)
            _room = result.Value;
        else if (result.Status == ResultStatus.NotFound)
            _errorMessage = "This room could not be found.";
        else
            _errorMessage = "Sorry, we could not load this room. Please try again later.";

        _isLoading = false;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadTextAsync();
    }
}
