using System.Globalization;
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

    // same format as the mockup, eg €432.00
    private static string FormatAmount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string FormatDate(DateTime date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    // Stripe sends lowercase brands like "visa" or "mastercard", eg Visa •••• 4242
    private static string FormatCard(string? brand, string last4)
    {
        var name = string.IsNullOrWhiteSpace(brand)
            ? "Card"
            : char.ToUpperInvariant(brand[0]) + brand[1..];
        return $"{name} •••• {last4}";
    }

    // TODO: drop "/test" once we use live Stripe keys
    private static string StripeDashboardUrl(string paymentIntent)
        => $"https://dashboard.stripe.com/test/payments/{paymentIntent}";
}
