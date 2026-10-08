using Rise.Shared.Common;

namespace Rise.Shared.Invoice;

public interface IInvoiceService
{
    Task<Result<InvoiceResponse.Index>> GetIndexAsync(QueryRequest.SkipTake request, CancellationToken ctx = default);
}
