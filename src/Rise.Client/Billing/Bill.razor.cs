using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
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

    /// <summary>where the admin came from, "invoices" when opened from the invoice list</summary>
    [SupplyParameterFromQuery(Name = "from")] public string? From { get; set; }

    private bool CameFromInvoices => From == "invoices";

    private string BackUrl => CameFromInvoices ? "admin/invoice-list" : $"admin/bookings/{BookingId}";

    private string BackLabel => CameFromInvoices ? "Invoices" : "Booking";

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

    // the settle button asks for a confirmation first, money is recorded
    private bool _confirmSettle;

    private bool _isSettling;

    // why the server refused the settlement, shown under the buttons
    private string? _settleError;

    private bool IsNewLine => _lineForm is { LineId: 0 };

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
        _confirmSettle = false;
        _settleError = null;

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

    // tells the admin how much will be collected, a credit or a paid bill collects nothing
    private string SettleQuestion => _bill!.OutstandingBalance > 0
        ? $"Collect {Format.Amount(_bill.OutstandingBalance)} on site and close this bill? It can't be changed afterwards."
        : "Close this bill? It can't be changed afterwards.";

    private void AskSettle()
    {
        _settleError = null;
        _confirmSettle = true;
    }

    private void KeepOpen()
    {
        if (!_isSettling)
            _confirmSettle = false;
    }

    private async Task SettleAsync()
    {
        _isSettling = true;
        _settleError = null;

        var result = await BillingService.SettleAsync(new BillingRequest.Settle { BookingId = BookingId });

        _isSettling = false;
        _confirmSettle = false;

        if (!result.IsSuccess)
        {
            _settleError = ErrorOf(result, "Sorry, the bill could not be settled. Please try again later.");
            return;
        }

        _bill = result.Value;
    }

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
