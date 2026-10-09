using Microsoft.AspNetCore.Components;
using Rise.Shared.Search;

namespace Rise.Client.Components;

/// <summary>
/// compact search bar like the one of the mockup, dates + check availability btn
/// used on top of the results page so the guest can change the dates without leaving
/// TODO: add guests and rooms fields when a story asks for them
/// </summary>
public partial class SearchBar
{
    /// <summary>
    /// arrival date already chosen, eg from the url of the results page
    /// </summary>
    [Parameter] public DateOnly? CheckIn { get; set; }

    /// <summary>
    /// departure date already chosen
    /// </summary>
    [Parameter] public DateOnly? CheckOut { get; set; }

    // dates of the form, the picker writes in here
    private readonly SearchRequest.StayPeriod _stay = new();

    // blazor service used to change page from the code
    [Inject] public required NavigationManager Navigation { get; set; }

    /// <summary>
    /// runs once when the bar appears, fills the form with the dates already chosen
    /// </summary>
    protected override void OnInitialized()
    {
        // copy the 2 parameters into _stay
        _stay.CheckIn = CheckIn;
        _stay.CheckOut = CheckOut;
    }

    // called by the form when the dates are valid, same url as Search/Index.razor.cs
    private void Search()
    {
        Navigation.NavigateTo($"/search/results?checkIn={_stay.CheckIn:yyyy-MM-dd}&checkOut={_stay.CheckOut:yyyy-MM-dd}");
    }
}
