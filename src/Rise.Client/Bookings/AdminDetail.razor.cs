using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// admin booking detail page, shows the booking, the guest and the stay, the admin can cancel some or all rooms here
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

    // form of the cancel dialog, the dialog is open while it is set, its rooms are the selected rooms
    private BookingRequest.Cancel? _cancelForm;

    private bool _isCancelling;

    // why the cancellation was refused, shown in the dialog
    private string? _cancelError;

    // shown above the booking once rooms are cancelled
    private string? _successMessage;

    // rooms still booked, only these can be cancelled
    private List<BookingDto.AdminRoomDto> ActiveRooms => _booking!.Rooms.Where(x => x.Cancellation is null).ToList();

    private List<BookingDto.AdminRoomDto> CancelledRooms => _booking!.Rooms.Where(x => x.Cancellation is not null).ToList();

    private bool AllActiveRoomsSelected => _cancelForm!.Rooms.Count == ActiveRooms.Count;

    private decimal SelectedRefundTotal => _cancelForm!.Rooms.Sum(x => x.RefundAmount);

    private bool IsRefundAboveLimit => SelectedRefundTotal > _booking!.RefundableAmount;

    // rooms left once the selected rooms are cancelled
    private List<BookingDto.AdminRoomDto> RemainingRooms
        => ActiveRooms.Where(room => _cancelForm!.Rooms.All(x => x.BookingRoomId != room.BookingRoomId)).ToList();

    private int RemainingCapacity => RemainingRooms.Sum(x => x.MaxGuests);

    // the guests no longer fit, so the admin has to enter the new nb, with no rooms left nobody has to fit
    private bool NeedsNewGuestCount => RemainingRooms.Count > 0 && _booking!.Adults > RemainingCapacity;

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
    /// opens the cancel dialog, cancelling is destructive so nothing is selected unless there is only 1 room
    /// </summary>
    private void OpenCancelDialog()
    {
        _cancelError = null;
        _cancelForm = new BookingRequest.Cancel { BookingId = BookingId };

        if (ActiveRooms is [var onlyRoom])
            SelectRoom(onlyRoom);
    }

    private void CloseCancelDialog()
    {
        if (!_isCancelling)
            _cancelForm = null;
    }

    private BookingRequest.Cancel.RoomSelection? SelectionOf(BookingDto.AdminRoomDto room)
        => _cancelForm!.Rooms.SingleOrDefault(x => x.BookingRoomId == room.BookingRoomId);

    private void ToggleRoom(BookingDto.AdminRoomDto room)
    {
        if (SelectionOf(room) is { } selection)
            _cancelForm!.Rooms.Remove(selection);
        else
            SelectRoom(room);

        ForgetUnneededGuestCount();
    }

    private void ToggleAllRooms()
    {
        if (AllActiveRoomsSelected)
            _cancelForm!.Rooms.Clear();
        else
            foreach (var room in ActiveRooms.Where(x => SelectionOf(x) is null))
                SelectRoom(room);

        ForgetUnneededGuestCount();
    }

    /// <summary>
    /// selects a room with the refund of its own policy, limited to what can still be refunded after the other selected rooms
    /// </summary>
    private void SelectRoom(BookingDto.AdminRoomDto room)
    {
        var left = Math.Max(_booking!.RefundableAmount - SelectedRefundTotal, 0m);

        _cancelForm!.Rooms.Add(new BookingRequest.Cancel.RoomSelection
        {
            BookingRoomId = room.BookingRoomId,
            RefundAmount = Math.Min(room.RefundAmount, left),
        });
    }

    // the new nb of guests is only sent when it is needed
    private void ForgetUnneededGuestCount()
    {
        if (!NeedsNewGuestCount)
            _cancelForm!.Adults = null;
    }

    private async Task CancelAsync()
    {
        // these rules need the booking, so the validator of the form can't check them, the server checks them again
        if (IsRefundAboveLimit)
        {
            _cancelError = $"The refunds can't be more than {Format.Amount(_booking!.RefundableAmount)}.";
            return;
        }

        if (NeedsNewGuestCount && (_cancelForm!.Adults is null || _cancelForm.Adults > RemainingCapacity))
        {
            _cancelError = $"Please enter the new number of guests, the remaining rooms fit at most {RemainingCapacity}.";
            return;
        }

        _isCancelling = true;
        _cancelError = null;

        var result = await BookingService.CancelAsync(_cancelForm!);

        _isCancelling = false;

        if (!result.IsSuccess)
        {
            // the server checks again, eg already cancelled in another tab or a refund above the amount paid
            _cancelError = result.ValidationErrors.Select(x => x.ErrorMessage)
                .Concat(result.Errors)
                .FirstOrDefault() ?? "Sorry, the cancellation failed. Please try again later.";
            return;
        }

        _successMessage = SuccessMessage(result.Value);
        _cancelForm = null;

        // reload to show the cancellation and the new status
        await LoadAsync();
    }

    private static string SuccessMessage(BookingResponse.Cancel cancelled)
    {
        var what = cancelled.RemainingRoomCount == 0
            ? $"Booking {cancelled.BookingNumber} is cancelled"
            : $"{cancelled.CancelledRoomCount} {(cancelled.CancelledRoomCount == 1 ? "room" : "rooms")} of booking {cancelled.BookingNumber} cancelled, {cancelled.RemainingRoomCount} still booked";

        return cancelled.RefundAmount > 0
            ? $"{what}. A refund of {Format.Amount(cancelled.RefundAmount)} is recorded, remember to pay it back yourself."
            : $"{what}, no refund is due.";
    }

    /// <summary>
    /// step of the refund timeline the reference date of the room falls in, 0 full refund, 1 partial refund, 2 no refund
    /// same day rules as the RefundCalculator on the server, the last day of a step still counts
    /// </summary>
    private static int RefundStep(BookingDto.AdminRoomDto room)
    {
        var date = room.RefundReferenceDate;
        if (room.FullRefundUntil is { } fullUntil && date <= fullUntil) return 0;
        if (room.PartialRefundUntil is { } partialUntil && date <= partialUntil) return 1;
        return 2;
    }

    // css class of a timeline step, the current step is highlighted and the past ones are faded
    private static string StepClass(int currentStep, int step)
        => step == currentStep ? "is-current" : step < currentStep ? "is-past" : string.Empty;
}
