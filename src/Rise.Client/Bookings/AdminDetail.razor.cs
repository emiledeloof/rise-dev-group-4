using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// admin booking detail page, shows the booking, the guest and the stay
/// </summary>
public partial class AdminDetail
{
    /// <summary>
    /// id of the booking from the url, eg /admin/bookings/3
    /// </summary>
    [Parameter] public int BookingId { get; set; }

    [Inject] public required IBookingService BookingService { get; set; }

    // booking from the api, null while loading or when it could not be loaded
    private BookingDto.AdminDetail? _booking;

    private bool _isLoading = true;

    // msg shown when there is no booking to show
    private string? _errorMessage;

    /// <summary>
    /// runs when the page opens and each time the id in the url changes, loads the booking
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _booking = null;
        _errorMessage = null;

        var result = await BookingService.GetDetailAsync(BookingId);

        if (result.IsSuccess)
            _booking = result.Value;
        else if (result.Status == ResultStatus.NotFound)
            _errorMessage = "This booking could not be found.";
        else
            _errorMessage = "Sorry, we could not load this booking. Please try again later.";

        _isLoading = false;
    }
}
