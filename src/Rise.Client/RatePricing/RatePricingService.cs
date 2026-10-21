using System.Net.Http.Json;
using Rise.Shared.RatePricing;

namespace Rise.Client.RatePricing;

/// <summary>
/// client side of IRatePricingService, sends the calls of the pricing page to the api over http
/// same idea as Invoice/InvoiceService.cs and Guests/GuestService.cs
/// </summary>
public class RatePricingService(HttpClient httpClient) : IRatePricingService
{
    /// <summary>
    /// GET /api/rate-pricing, all rates with their pricing
    /// </summary>
    public async Task<Result<RatePricingResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        // GetFromJsonAsync on /api/rate-pricing, read the body as a Result of RatePricingResponse.Index
        // same as InvoiceService, the ! says the body is never null
        return (await httpClient.GetFromJsonAsync<Result<RatePricingResponse.Index>>(
            $"/api/rate-pricing",
            cancellationToken: ctx))!;
    }

    /// <summary>
    /// PUT /api/rate-pricing with the new price in the body
    /// </summary>
    public async Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default)
    {
        // send the request as json with PutAsJsonAsync on /api/rate-pricing, keep the http response in a var
        var response = await httpClient.PutAsJsonAsync("/api/rate-pricing", request, ctx);

        // read the body of the response as a Result with ReadFromJsonAsync, same as GuestService
        var result = await response.Content.ReadFromJsonAsync<Result>(cancellationToken: ctx);

        // return it, ! because the server always sends a body
        return result!;
    }
}
