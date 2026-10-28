using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// admin booking detail page, shows the booking, the guest and the stay, the admin can cancel the booking here
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

    // form of the cancel dialog, the dialog is open while it is set
    private BookingRequest.Cancel? _cancelForm;

    private bool _isCancelling;

    // why the server refused the cancellation, shown in the dialog
    private string? _cancelError;

    // shown above the booking once it is cancelled
    private string? _successMessage;

    /// <summary>
    /// runs when the page opens and each time the id in the url changes, loads the booking
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _booking = null;
        _errorMessage = null;
        _successMessage = null;
        _cancelForm = null;

        await LoadAsync();

        _isLoading = false;
    }

    private async Task LoadAsync()
    {
        var result = await BookingService.GetDetailAsync(BookingId);

        if (result.IsSuccess)
            _booking = result.Value;
        else if (result.Status == ResultStatus.NotFound)
            _errorMessage = "This booking could not be found.";
        else
            _errorMessage = "Sorry, we could not load this booking. Please try again later.";
    }

    /// <summary>
    /// opens the cancel dialog with the refund due per the cancellation policy filled in
    /// </summary>
    private void OpenCancelDialog()
    {
        _cancelError = null;
        _cancelForm = new BookingRequest.Cancel
        {
            BookingId = BookingId,
            RefundAmount = _booking!.SuggestedRefundAmount,
        };
    }

    private void CloseCancelDialog()
    {
        if (!_isCancelling)
            _cancelForm = null;
    }

    private async Task CancelAsync()
    {
        _isCancelling = true;
        _cancelError = null;

        var result = await BookingService.CancelAsync(_cancelForm!);

        _isCancelling = false;

        if (!result.IsSuccess)
        {
            // the server checks again, eg already cancelled in another tab or a refund above the amount paid
            _cancelError = result.ValidationErrors.Select(x => x.ErrorMessage)
                .Concat(result.Errors)
                .FirstOrDefault() ?? "Sorry, the booking could not be cancelled. Please try again later.";
            return;
        }

        var cancelled = result.Value;
        _successMessage = cancelled.RefundAmount > 0
            ? $"Booking {cancelled.BookingNumber} is cancelled. A refund of {Format.Amount(cancelled.RefundAmount)} is recorded, remember to pay it back yourself."
            : $"Booking {cancelled.BookingNumber} is cancelled, no refund is due.";
        _cancelForm = null;

        // reload to show the cancellation and the new status
        await LoadAsync();
    }
}
