using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Deletes an on-site payment recorded by mistake, returns the whole bill with the new balance.
/// </summary>
public sealed class DeletePayment(IBillingService billingService)
    : Endpoint<BillingRequest.DeletePayment, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Delete("/api/bookings/{BookingId}/bill/payments/{PaymentId}");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.DeletePayment req, CancellationToken ct)
    {
        return billingService.DeletePaymentAsync(req, ct);
    }
}
