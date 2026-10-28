using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Components;

public partial class BookingPopup
{
    [Parameter] public int? BookingId { get; set; }
    [Parameter] public double X { get; set; }
    [Parameter] public double Y { get; set; }

    [Inject] public required IBookingService BookingService { get; set; }

    private BookingDto.AdminDetail? _detail;
    private bool _loading;
    private string? _errorMessage;
    private int? _lastFetchedId;

    protected override async Task OnParametersSetAsync()
    {
        if (BookingId is null)
        {
            _detail = null;
            _lastFetchedId = null;
            _errorMessage = null;
            _loading = false;
            return;
        }

        if (BookingId == _lastFetchedId) return;

        var targetId = BookingId.Value;
        _lastFetchedId = targetId;
        _loading = true;
        _detail = null;
        _errorMessage = null;

        var result = await BookingService.GetDetailAsync(targetId);

        if (BookingId != targetId) return;

        _loading = false;

        if (result.IsSuccess)
        {
            _detail = result.Value;
        }
        else
        {
            _errorMessage = "Details unavailable";
        }
    }
}
