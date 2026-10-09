using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;
using Rise.Shared.Search;
using Rise.Client.Components;

namespace Rise.Client.Search;

/// <summary>
/// results page of the stay search, shows the rooms free for the chosen dates
/// </summary>
public partial class Results
{
    /// <summary>
    /// arrival date read from the url, eg ?checkIn=2026-10-14
    /// SupplyParameterFromQuery fills it automatically from the query string
    /// </summary>
    [SupplyParameterFromQuery] private DateOnly? CheckIn { get; set; }

    /// <summary>
    /// departure date read from the url, eg checkOut=2026-10-17
    /// </summary>
    [SupplyParameterFromQuery] private DateOnly? CheckOut { get; set; }

    // dates of the url grouped in a stay period, to reuse its Nights calculation
    private SearchRequest.StayPeriod Stay => new() { CheckIn = CheckIn, CheckOut = CheckOut };

    // blazor service used to change page from the code, same as in Search/Index.razor.cs
    [Inject] public required NavigationManager Navigation { get; set; }

    // client RoomService registered in Program.cs, calls the availability endpoint
    [Inject] public required IRoomService RoomService { get; set; }

    // free rooms from the api, null while the page is still loading
    private IReadOnlyList<RoomDto.Available>? _rooms;

    // msg shown when the api call fails, null when all is fine
    private string? _errorMessage;

    // rooms the guest picked, key = id of the room, value = id of the chosen rate
    // a dictionary so a room can only be there once, with one rate
    private readonly Dictionary<int, int> _selection = [];

    /// <summary>
    /// runs when the page opens and each time the dates in the url change, checks the dates then loads the free rooms
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // reset so the page shows loading again for the new dates
        _rooms = null;
        _errorMessage = null;

        // new dates mean new prices, so the old selection is not valid anymore
        _selection.Clear();

        // same validator as the search form, so the rules are written only once
        // Validate checks every rule on Stay and returns a result
        // validation.IsValid is true when all rules pass, false when at least one fails
        var validation = new SearchRequest.StayPeriod.Validator().Validate(Stay);

        // back to the search page when a rule fails, eg no dates or check out before check in
        // like a bouncer at the door, no valid dates means back to the search form
        if (!validation.IsValid)
        {
            Navigation.NavigateTo("/search");

            // stop here, no need to call the api with bad dates
            return;
        }

        // ask the api for the free rooms of the stay
        var result = await RoomService.GetAvailableAsync(Stay);

        // the call failed, show a msg and an empty list instead of crashing
        if (!result.IsSuccess)
        {
            _errorMessage = "Sorry, we could not load the rooms. Please try again later.";
            _rooms = [];
            return;
        }

        // all good, keep the rooms so the page can show them
        _rooms = result.Value.Rooms.ToList();
    }

    // called by a card when the guest picks a rate, adds the room or changes its rate
    private void SelectRate(int roomId, int rateId)
    {
        _selection[roomId] = rateId;
    }

    // called by a card when the guest clicks remove
    private void RemoveRoom(int roomId)
    {
        _selection.Remove(roomId);
    }

    // lines of the summary, built from the selection and the rooms of the api
    private IReadOnlyList<SelectedRoom> SelectedRooms => _rooms is null ? [] :
        _selection.Select(s =>
        {
            // the room and the rate behind the 2 ids of the selection
            var room = _rooms.First(r => r.Id == s.Key);
            var rate = room.Rates.First(x => x.Id == s.Value);
            return new SelectedRoom(room.Name, rate.Name, rate.TotalPrice);
    }).ToList();

    // rate chosen for a room, null when the room is not in the selection
    // TryGetValue gives true and the value when the key is there, false otherwise
    private int? SelectedRateOf(int roomId) => _selection.TryGetValue(roomId, out var rateId) ? rateId : null;
}
