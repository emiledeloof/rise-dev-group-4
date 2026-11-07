using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Records a payment taken on site, eg cash at the desk, returns the whole bill with the new balance.
/// </summary>
public sealed class RecordPayment(IBillingService billingService)
    : Endpoint<BillingRequest.RecordPayment, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/bill/payments");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.RecordPayment req, CancellationToken ct)
    {
        return billingService.RecordPaymentAsync(req, ct);
    }
}
