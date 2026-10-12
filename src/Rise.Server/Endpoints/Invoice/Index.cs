using Rise.Shared.Common;
using Rise.Shared.Identity;
using Rise.Shared.Invoice;

namespace Rise.Server.Endpoints.Invoice
{
    public class Index(IInvoiceService invoiceService) : Endpoint<QueryRequest.SkipTake, Result<InvoiceResponse.Index>>
    {
        public override void Configure()
        {
            Get("/api/invoices");
            Roles(AppRoles.Administrator);
        }

        public override Task<Result<InvoiceResponse.Index>> ExecuteAsync(QueryRequest.SkipTake req, CancellationToken ct)
        {
            return invoiceService.GetIndexAsync(req, ct);
        }
    }
}
