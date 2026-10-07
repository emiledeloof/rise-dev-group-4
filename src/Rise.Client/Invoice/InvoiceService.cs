using System.Net.Http.Json;
using Rise.Shared.Common;
using Rise.Shared.Invoice;

namespace Rise.Client.Invoice;

public class InvoiceService(HttpClient httpClient) : IInvoiceService
{
    public async Task<Result<InvoiceResponse.Index>> GetIndexAsync(QueryRequest.SkipTake request, CancellationToken ctx = default)
    {
        return (await httpClient.GetFromJsonAsync<Result<InvoiceResponse.Index>>(
            $"/api/invoices{request}",
            cancellationToken: ctx))!;
    }
}
