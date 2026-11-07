using Rise.Services.Billing;
using Rise.Shared.Billing;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Billing;

/// <summary>returns the final bill of a booking as a pdf, opened in the browser to print or save it</summary>
public sealed class Pdf(IBillingService billingService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/bookings/{bookingId}/bill/pdf");
        Roles(AppRoles.Administrator);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await billingService.GetBillAsync(Route<int>("bookingId"), ct);
        if (!result.IsSuccess)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // no file name, so the browser shows the pdf instead of downloading it
        await Send.BytesAsync(BillPdf.Create(result.Value), contentType: "application/pdf", cancellation: ct);
    }
}
