using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Deletes a free line of the bill, returns the whole bill with the new totals.
/// </summary>
public sealed class DeleteLine(IBillingService billingService)
    : Endpoint<BillingRequest.DeleteLine, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Delete("/api/bookings/{BookingId}/bill/lines/{LineId}");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.DeleteLine req, CancellationToken ct)
    {
        return billingService.DeleteLineAsync(req, ct);
    }
}
