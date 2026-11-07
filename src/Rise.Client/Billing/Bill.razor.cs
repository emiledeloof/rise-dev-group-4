using Microsoft.AspNetCore.Components;
using Rise.Shared.Billing;

namespace Rise.Client.Billing;

/// <summary>
/// admin final bill of a booking, what the guest used, what is paid and what is still open
/// the admin can add, change and delete free lines like minibar here
/// </summary>
public partial class Bill
{
    /// <summary>
    /// id of the booking from the url, eg /admin/bookings/3/bill
    /// </summary>
    [Parameter] public int BookingId { get; set; }

    [Inject] public required IBillingService BillingService { get; set; }

    // bill from the api, null while loading or when it could not be loaded
    private BillingDto.Bill? _bill;

    private bool _isLoading = true;

    // msg shown when there is no bill to show
    private string? _errorMessage;

    // form of the line sheet, the sheet is open while it is set, LineId 0 means a new line
    private BillingRequest.EditLine? _lineForm;

    private bool _isSaving;

    // why the server refused the line, shown in the sheet
    private string? _lineError;

    // line that asks "delete this line?", only 1 at a time
    private int? _confirmDeleteId;

    private bool _isDeleting;

    // why the server refused the delete, shown under the line
    private string? _deleteError;

    // form of the payment sheet, the sheet is open while it is set
    private BillingRequest.RecordPayment? _paymentForm;

    private bool _isRecording;

    // why the server refused the payment, shown in the sheet
    private string? _paymentError;

    // on-site payment that asks "delete this payment?", only 1 at a time
    private int? _confirmDeletePaymentId;

    private bool _isDeletingPayment;

    private string? _deletePaymentError;

    private bool IsNewLine => _lineForm is { LineId: 0 };

    // a payment only makes sense while something is still open
    private bool CanRecordPayment => _bill is { IsCancelled: false, OutstandingBalance: > 0 };

    private decimal BalanceAfterPayment => _bill!.OutstandingBalance - (_paymentForm?.Amount ?? 0m);

    private List<BillingDto.AccommodationLine> CancelledRooms => _bill!.Accommodation.Where(x => x.IsCancelled).ToList();

    // rooms can be cancelled on different days, the booking is only fully cancelled from the last one on
    private DateTime LastCancelledAt => CancelledRooms.Max(x => x.Cancellation!.CancelledAt);

    /// <summary>
    /// runs when the page opens and each time the id in the url changes, loads the bill
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _bill = null;
        _errorMessage = null;
        _lineForm = null;
        _confirmDeleteId = null;
        _paymentForm = null;
        _confirmDeletePaymentId = null;

        var result = await BillingService.GetBillAsync(BookingId);

        if (result.IsSuccess)
            _bill = result.Value;
        else if (result.Status == ResultStatus.NotFound)
            _errorMessage = "This booking could not be found.";
        else
            _errorMessage = "Sorry, we could not load this bill. Please try again later.";

        _isLoading = false;
    }

    private void OpenAddLine()
    {
        _lineError = null;
        _confirmDeleteId = null;
        _lineForm = new BillingRequest.EditLine { BookingId = BookingId };
    }

    private void OpenEditLine(BillingDto.OtherServiceLine line)
    {
        _lineError = null;
        _confirmDeleteId = null;
        _lineForm = new BillingRequest.EditLine
        {
            BookingId = BookingId,
            LineId = line.Id,
            Description = line.Description,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            // the db gives eg 22.00, the dropdown only knows 22
            VatPercentage = VatRates.Presets.FirstOrDefault(x => x == line.VatPercentage, VatRates.Default),
        };
    }

    private void CloseLineSheet()
    {
        if (!_isSaving)
            _lineForm = null;
    }

    private async Task SaveLineAsync()
    {
        _isSaving = true;
        _lineError = null;

        // a new line is sent as AddLine, so the json has no LineId
        var result = IsNewLine
            ? await BillingService.AddLineAsync(_lineForm!)
            : await BillingService.EditLineAsync(_lineForm!);

        _isSaving = false;

        if (!result.IsSuccess)
        {
            _lineError = ErrorOf(result, "Sorry, the line could not be saved. Please try again later.");
            return;
        }

        _bill = result.Value;
        _lineForm = null;
    }

    private void AskDelete(int lineId)
    {
        _deleteError = null;
        _confirmDeleteId = lineId;
    }

    private void KeepLine()
    {
        if (!_isDeleting)
            _confirmDeleteId = null;
    }

    private async Task DeleteLineAsync(int lineId)
    {
        _isDeleting = true;
        _deleteError = null;

        var result = await BillingService.DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = BookingId, LineId = lineId });

        _isDeleting = false;

        if (!result.IsSuccess)
        {
            _deleteError = ErrorOf(result, "Sorry, the line could not be deleted. Please try again later.");
            return;
        }

        _bill = result.Value;
        _confirmDeleteId = null;
    }

    /// <summary>
    /// opens the payment sheet with the whole open balance, the most common case at checkout
    /// </summary>
    private void OpenRecordPayment()
    {
        _paymentError = null;
        _confirmDeletePaymentId = null;
        _paymentForm = new BillingRequest.RecordPayment
        {
            BookingId = BookingId,
            Amount = _bill!.OutstandingBalance,
        };
    }

    private void ClosePaymentSheet()
    {
        if (!_isRecording)
            _paymentForm = null;
    }

    private async Task RecordPaymentAsync()
    {
        _isRecording = true;
        _paymentError = null;

        var result = await BillingService.RecordPaymentAsync(_paymentForm!);

        _isRecording = false;

        if (!result.IsSuccess)
        {
            _paymentError = ErrorOf(result, "Sorry, the payment could not be recorded. Please try again later.");
            return;
        }

        _bill = result.Value;
        _paymentForm = null;
    }

    private void AskDeletePayment(int paymentId)
    {
        _deletePaymentError = null;
        _confirmDeletePaymentId = paymentId;
    }

    private void KeepPayment()
    {
        if (!_isDeletingPayment)
            _confirmDeletePaymentId = null;
    }

    private async Task DeletePaymentAsync(int paymentId)
    {
        _isDeletingPayment = true;
        _deletePaymentError = null;

        var result = await BillingService.DeletePaymentAsync(new BillingRequest.DeletePayment { BookingId = BookingId, PaymentId = paymentId });

        _isDeletingPayment = false;

        if (!result.IsSuccess)
        {
            _deletePaymentError = ErrorOf(result, "Sorry, the payment could not be deleted. Please try again later.");
            return;
        }

        _bill = result.Value;
        _confirmDeletePaymentId = null;
    }

    private static string MethodIcon(string method) => method switch
    {
        PaymentMethods.Cash => "fa-money-bill-wave",
        PaymentMethods.CardTerminal => "fa-credit-card",
        PaymentMethods.BankTransfer => "fa-building-columns",
        _ => "fa-globe",
    };

    // the server checks again, eg the booking was cancelled in another tab
    private static string ErrorOf(Result<BillingDto.Bill> result, string fallback)
        => result.ValidationErrors.Select(x => x.ErrorMessage)
            .Concat(result.Errors)
            .FirstOrDefault() ?? fallback;

    // css class of the balance, highlighted while something is still open
    private string BalanceClass => _bill!.OutstandingBalance switch
    {
        > 0 => "is-open",
        < 0 => "is-credit",
        _ => "is-settled",
    };

    private string BalanceLabel => _bill!.OutstandingBalance switch
    {
        > 0 => "Outstanding",
        < 0 => "Credit",
        _ => "Settled",
    };
}
