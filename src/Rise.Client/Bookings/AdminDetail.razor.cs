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

    // shown above the booking once rooms are cancelled or a refund payment changed
    private string? _successMessage;

    // form to record the refund payment of 1 cancelled room, open under that room while it is set
    private BookingRequest.RecordRefund? _refundForm;

    private bool _isSavingRefund;

    // why recording or undoing the refund payment was refused
    private string? _refundError;

    // room whose refund payment the admin is about to undo, asks to confirm first
    private int? _undoRoomId;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // rooms still booked, only these can be cancelled
    private List<BookingDto.AdminRoomDto> ActiveRooms => _booking!.Rooms.Where(x => x.Cancellation is null).ToList();

    private List<BookingDto.AdminRoomDto> CancelledRooms => _booking!.Rooms.Where(x => x.Cancellation is not null).ToList();

    // ! an open checkout created before a cancellation still charges the old amount, a new one only the remaining rooms,
    // so compare each payment with the rooms not yet cancelled when it was paid, rooms cancelled later were rightly charged
    private decimal OverpaidAmount => _booking!.Payments
        .Where(x => x.PaidAt is not null)
        .Sum(payment => Math.Max(payment.Amount - _booking.Rooms
            .Where(room => room.Cancellation is null || room.Cancellation.CancelledAt >= payment.PaidAt)
            .Sum(room => room.Total), 0m));

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
        _refundForm = null;
        _undoRoomId = null;

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
    /// opens the form under the room, prefilled with today and the decided refund, the admin can change both
    /// </summary>
    private void OpenRefundForm(BookingDto.AdminRoomDto room)
    {
        _refundError = null;
        _undoRoomId = null;
        _refundForm = new BookingRequest.RecordRefund
        {
            BookingId = BookingId,
            BookingRoomId = room.BookingRoomId,
            PaidOn = Today,
            Amount = room.Cancellation!.RefundAmount,
        };
    }

    private void CloseRefundForm()
    {
        if (!_isSavingRefund)
            _refundForm = null;
    }

    private async Task RecordRefundAsync()
    {
        var form = _refundForm!;
        var roomNumber = RoomNumberOf(form.BookingRoomId);
        _isSavingRefund = true;
        _refundError = null;

        var result = await BookingService.RecordRefundAsync(form);

        _isSavingRefund = false;

        if (!result.IsSuccess)
        {
            _refundError = ErrorOf(result) ?? "Sorry, the refund payment could not be saved. Please try again later.";
            return;
        }

        _refundForm = null;
        _successMessage = $"The refund of {Format.Amount(form.Amount)} for room {roomNumber} is recorded as paid.";
        await LoadAsync();
    }

    private void AskUndoRefund(BookingDto.AdminRoomDto room)
    {
        _refundError = null;
        _refundForm = null;
        _undoRoomId = room.BookingRoomId;
    }

    private async Task UndoRefundAsync(BookingDto.AdminRoomDto room)
    {
        _isSavingRefund = true;
        _refundError = null;

        var result = await BookingService.UndoRefundAsync(new BookingRequest.UndoRefund
        {
            BookingId = BookingId,
            BookingRoomId = room.BookingRoomId,
        });

        _isSavingRefund = false;

        if (!result.IsSuccess)
        {
            // stays under the confirmation of this room
            _refundError = ErrorOf(result) ?? "Sorry, the refund payment could not be undone. Please try again later.";
            return;
        }

        _undoRoomId = null;
        _successMessage = $"The refund of room {room.RoomNumber} is open again.";
        await LoadAsync();
    }

    private string RoomNumberOf(int bookingRoomId)
        => _booking!.Rooms.Single(x => x.BookingRoomId == bookingRoomId).RoomNumber;

    // first msg of a refused Result, the server checks again, eg already paid in another tab
    private static string? ErrorOf<T>(Result<T> result)
        => result.ValidationErrors.Select(x => x.ErrorMessage).Concat(result.Errors).FirstOrDefault();

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
