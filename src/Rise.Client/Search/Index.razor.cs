namespace Rise.Client.Search;

/// <summary>
/// stay search page, first step of a booking
/// the form and the navigation to the results page live in the SearchBar component
/// </summary>
public partial class Index
{
    // dates picked in the calendar, reused later to search the available rooms
    private readonly SearchRequest.StayPeriod _stay = new();

    // blazor service used to change page from the code
    // [Inject] gives it to us automatically
    [Inject] public required NavigationManager Navigation { get; set; }


    // called by the form when the dates are valid, goes to the results page with the dates in the url
    private Task SearchAsync()
    {
        // dates in the url as yyyy-MM-dd, eg /search/results?checkIn=2026-10-14&checkOut=2026-10-17
        // so the results page of subtask 211 can read them
        Navigation.NavigateTo($"/search/results?checkIn={_stay.CheckIn:yyyy-MM-dd}&checkOut={_stay.CheckOut:yyyy-MM-dd}");
        return Task.CompletedTask;
    }
}
