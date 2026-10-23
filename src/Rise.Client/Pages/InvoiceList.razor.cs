using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.Invoice;

namespace Rise.Client.Pages;

public partial class InvoiceList
{
    private IEnumerable<InvoiceDto.Index>? invoices;
    private string? errorMessage;

    [Inject] public required IInvoiceService InvoiceService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var request = new QueryRequest.SkipTake
        {
            Skip = 0,
            Take = 50,
            OrderBy = "Id",
            OrderDescending = true,
        };

        var result = await InvoiceService.GetIndexAsync(request);

        if (!result.IsSuccess)
        {
            errorMessage = "The invoices could not be loaded.";
            invoices = [];
            return;
        }

        invoices = result.Value.Invoices;
    }

    // TODO: drop "/test" once we use live Stripe keys
    private static string StripeDashboardUrl(string paymentIntent)
        => $"https://dashboard.stripe.com/test/payments/{paymentIntent}";
}
