using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Adds a free line to the bill, eg minibar, returns the whole bill with the new totals.
/// </summary>
public sealed class AddLine(IBillingService billingService)
    : Endpoint<BillingRequest.AddLine, Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/bill/lines");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(BillingRequest.AddLine req, CancellationToken ct)
    {
        return billingService.AddLineAsync(req, ct);
    }
}
