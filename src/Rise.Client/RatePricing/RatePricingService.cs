using System.Text.Json;
using System.Net.Http.Json;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;

namespace Rise.Client.RatePricing;

/// <summary>
/// client side of IRatePricingService, sends the edit form of the rates page to the api over http
/// same idea as Invoice/InvoiceService.cs and Guests/GuestService.cs
/// </summary>
public class RatePricingService(HttpClient httpClient) : IRatePricingService
{
    public async Task<Result<GeneralSettingsDto>> GetGeneralSettingsAsync(CancellationToken ctx = default)
    {
        return await httpClient.GetFromJsonAsync<Result<GeneralSettingsDto>>(
                   "/api/rate-pricing/settings",
                   ctx)
               ?? throw new JsonException("The rate-pricing settings response was empty.");
    }

    public async Task<Result<GeneralSettingsDto>> UpdateGeneralSettingsAsync(
        GeneralSettingsRequest.Update request,
        CancellationToken ctx = default)
    {
        using var response = await httpClient.PutAsJsonAsync("/api/rate-pricing/settings", request, ctx);
        return await response.Content.ReadFromJsonAsync<Result<GeneralSettingsDto>>(cancellationToken: ctx)
               ?? throw new JsonException("The rate-pricing settings response was empty.");
    }

    /// <summary>
    /// PUT /api/rate-pricing with the new pricing in the body
    /// </summary>
    public async Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default)
    {
        // ! PUT, same verb as the endpoint, a POST would give a 405
        var response = await httpClient.PutAsJsonAsync("/api/rate-pricing", request, ctx);

        // the body holds the Result even for a 400 or 404, so the page can show the reason
        var result = await response.Content.ReadFromJsonAsync<Result>(cancellationToken: ctx);
        return result!;
    }
}
