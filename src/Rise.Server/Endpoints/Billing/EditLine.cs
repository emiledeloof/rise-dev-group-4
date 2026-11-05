using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Changes a free line of the bill, returns the whole bill with the new totals.
/// </summary>
public sealed class EditLine(IBillingService billingService)
    : Endpoint<BillingRequest.EditLine, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Put("/api/bookings/{BookingId}/bill/lines/{LineId}");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.EditLine req, CancellationToken ct)
    {
        return billingService.EditLineAsync(req, ct);
    }
}
