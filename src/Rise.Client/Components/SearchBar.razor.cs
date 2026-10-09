using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Shared.Search;

namespace Rise.Client.Components;

/// <summary>
/// compact search bar like the one of the mockup, dates + check availability btn
/// used on the home, search and results pages so the bar looks the same everywhere
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

    // state of the form, which fields have errors, created once in OnInitialized
    // the EditForm gets it instead of a Model so we can ask it to check again
    private EditContext _editContext = default!;

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

        // the form state works on _stay, same as Model="_stay" before
        _editContext = new EditContext(_stay);
    }

    // called after each date change in the picker
    // without it the old errors stay on screen until the next click on the btn
    private void DatesChanged()
    {
        // only check again once both dates are picked, so no error pops up while the guest is still choosing
        // Validate runs the StayPeriod rules again and clears the messages when they pass
        if (_stay.CheckIn is not null && _stay.CheckOut is not null)
        {
            _editContext.Validate();
        }
    }

    // called by the form when the dates are valid, same url as before
    private void Search()
    {
        Navigation.NavigateTo($"/search/results?checkIn={_stay.CheckIn:yyyy-MM-dd}&checkOut={_stay.CheckOut:yyyy-MM-dd}");
    }
}
