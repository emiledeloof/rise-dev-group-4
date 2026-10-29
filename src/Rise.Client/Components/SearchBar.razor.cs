using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Client.Components;

/// <summary>
/// compact search bar like the one of the mockup, dates + guests + rooms + check availability btn
/// used on the home, search and results pages so the bar looks the same everywhere
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

    /// <summary>
    /// number of adult guests already chosen, eg from the url
    /// </summary>
    [Parameter] public int? Adults { get; set; }

    /// <summary>
    /// number of rooms already chosen, eg from the url
    /// </summary>
    [Parameter] public int? Rooms { get; set; }

    /// <summary>
    /// maximum number of adult guests allowed (optional, can be passed from parent or fetched dynamically)
    /// </summary>
    [Parameter] public int? MaxGuests { get; set; }

    /// <summary>
    /// maximum number of rooms allowed (optional, can be passed from parent or fetched dynamically)
    /// </summary>
    [Parameter] public int? MaxRooms { get; set; }

    // optional service provider to dynamically fetch estate capacity and room count when not passed as parameters
    [Inject] private IServiceProvider? ServiceProvider { get; set; }

    // mutual exclusion of popups so calendar and guest picker never overlap
    private bool _isCalendarOpen;
    private bool _isGuestPickerOpen;

    private void OnCalendarOpenChanged(bool isOpen)
    {
        _isCalendarOpen = isOpen;
        if (isOpen)
            _isGuestPickerOpen = false;
    }

    private void OnGuestPickerOpenChanged(bool isOpen)
    {
        _isGuestPickerOpen = isOpen;
        if (isOpen)
            _isCalendarOpen = false;
    }

    // dates, guest and room count of the form
    private readonly SearchRequest.StayPeriod _stay = new();

    // state of the form, which fields have errors, created once in OnInitialized
    // the EditForm gets it instead of a Model so we can ask it to check again
    private EditContext _editContext = default!;

    // blazor service used to change page from the code
    [Inject] public required NavigationManager Navigation { get; set; }

    /// <summary>
    /// runs once when the bar appears, fills the form with the values already chosen
    /// </summary>
    protected override void OnInitialized()
    {
        // copy the parameters into _stay
        _stay.CheckIn = CheckIn;
        _stay.CheckOut = CheckOut;
        _stay.Adults = Adults ?? SearchRequest.StayPeriod.DefaultAdults;
        _stay.Rooms = Rooms ?? SearchRequest.StayPeriod.DefaultRooms;

        if (MaxGuests.HasValue)
            _stay.MaxCapacity = MaxGuests.Value;

        if (MaxRooms.HasValue)
            _stay.MaxRooms = MaxRooms.Value;

        // the form state works on _stay, same as Model="_stay" before
        _editContext = new EditContext(_stay);
    }

    /// <summary>
    /// updates dynamic capacities when parent parameters change
    /// </summary>
    protected override void OnParametersSet()
    {
        if (MaxGuests.HasValue)
            _stay.MaxCapacity = MaxGuests.Value;

        if (MaxRooms.HasValue)
            _stay.MaxRooms = MaxRooms.Value;
    }

    /// <summary>
    /// dynamically loads the estate capacity and room count from the database if not provided
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        if (!_stay.MaxCapacity.HasValue || !_stay.MaxRooms.HasValue)
        {
            var roomService = ServiceProvider?.GetService<IRoomService>();
            if (roomService is not null)
            {
                try
                {
                    var result = await roomService.GetListAsync(new RoomRequest.List { Take = 0 });
                    if (result.IsSuccess)
                    {
                        if (!_stay.MaxRooms.HasValue && result.Value.TotalCount > 0)
                            _stay.MaxRooms = result.Value.TotalCount;

                        if (!_stay.MaxCapacity.HasValue && result.Value.TotalCapacity > 0)
                            _stay.MaxCapacity = result.Value.TotalCapacity;
                    }
                }
                catch
                {
                    // offline or in tests without IRoomService
                }
            }
        }
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

    // called by the form when the inputs are valid, navigates to results with dates, adults and rooms in url
    private void Search()
    {
        Navigation.NavigateTo($"/search/results?checkIn={_stay.CheckIn:yyyy-MM-dd}&checkOut={_stay.CheckOut:yyyy-MM-dd}&adults={_stay.Adults}&rooms={_stay.Rooms}");
    }
}
