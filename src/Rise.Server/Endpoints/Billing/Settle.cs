using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>settles the bill of a booking, returns the closed bill with the final payment</summary>
public sealed class Settle(IBillingService billingService)
    : Endpoint<BillingRequest.Settle, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/bill/settle");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.Settle req, CancellationToken ct)
    {
        return billingService.SettleAsync(req, ct);
    }
}
