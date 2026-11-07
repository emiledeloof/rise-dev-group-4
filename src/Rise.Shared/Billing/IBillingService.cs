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

    /// <summary>closes the bill and records the outstanding balance as paid on site, a bill is settled only once</summary>
    Task<Result<BillingDto.Bill>> SettleAsync(BillingRequest.Settle request, CancellationToken ctx = default);
}
