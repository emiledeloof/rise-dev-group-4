using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>
/// Returns the final bill of a booking for the admin bill page.
/// </summary>
public sealed class Detail(IBillingService billingService)
    : EndpointWithoutRequest<Result<BillingDto.Bill>>
{
    public override void Configure()
    {
        Get("/api/bookings/{bookingId}/bill");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BillingDto.Bill>> ExecuteAsync(CancellationToken ct)
    {
        var bookingId = Route<int>("bookingId");
        return billingService.GetBillAsync(bookingId, ct);
    }
}
