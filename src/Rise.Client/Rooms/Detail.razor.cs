using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;

namespace Rise.Client.Rooms;

/// <summary>
/// room detail page, shows what the room offers, opened from the search results
/// </summary>
public partial class Detail
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
    private string BackUrl => HasSearchDates ? $"search/results?checkIn={CheckIn:yyyy-MM-dd}&checkOut={CheckOut:yyyy-MM-dd}" : "search";
    private string BackText => HasSearchDates ? "Back to available rooms" : "Search rooms";

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
}
