using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

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

    /// <summary>
    /// runs when the page opens and each time the dates in the url change, checks the dates then loads the free rooms
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // reset so the page shows loading again for the new dates
        _rooms = null;
        _errorMessage = null;

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
}
