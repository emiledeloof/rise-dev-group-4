using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.Invoice;

namespace Rise.Client.Identity;

public partial class AdminLandpage : ComponentBase
{
    private IEnumerable<InvoiceDto.Index>? recentInvoices;
    private int invoiceCount;
    private string? invoiceError;
    private int paidInvoiceCount;
    private int openInvoiceCount;

    [Inject] public required IInvoiceService InvoiceService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var result = await InvoiceService.GetIndexAsync(new QueryRequest.SkipTake
        {
            Skip = 0,
            Take = 3,
            OrderBy = "Id",
            OrderDescending = true
        });

        if (!result.IsSuccess)
        {
            invoiceError = "Invoices could not be loaded.";
            recentInvoices = [];
            return;
        }

        invoiceCount = result.Value.TotalCount;
        recentInvoices = result.Value.Invoices;
    }
}