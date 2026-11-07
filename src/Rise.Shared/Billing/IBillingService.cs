namespace Rise.Shared.Billing;

/// <summary>
/// The final bill of a booking for the admin: what the guest used, what is paid and what is still open.
/// Every call returns the whole bill, so the page can show the new totals right after a change.
/// </summary>
public interface IBillingService
{
    Task<Result<BillingDto.Bill>> GetBillAsync(int bookingId, CancellationToken ctx = default);

    /// <summary>
    /// Adds a free line, eg minibar, the bill of a cancelled booking can't be changed.
    /// </summary>
    Task<Result<BillingDto.Bill>> AddLineAsync(BillingRequest.AddLine request, CancellationToken ctx = default);

    Task<Result<BillingDto.Bill>> EditLineAsync(BillingRequest.EditLine request, CancellationToken ctx = default);

    Task<Result<BillingDto.Bill>> DeleteLineAsync(BillingRequest.DeleteLine request, CancellationToken ctx = default);

    /// <summary>
    /// Records a payment taken on site, at most the outstanding balance, not on a cancelled booking.
    /// </summary>
    Task<Result<BillingDto.Bill>> RecordPaymentAsync(BillingRequest.RecordPayment request, CancellationToken ctx = default);

    /// <summary>
    /// Soft deletes an on-site payment recorded by mistake, Stripe payments stay.
    /// </summary>
    Task<Result<BillingDto.Bill>> DeletePaymentAsync(BillingRequest.DeletePayment request, CancellationToken ctx = default);
}
