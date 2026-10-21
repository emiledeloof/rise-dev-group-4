using System.Net.Http.Json;
using Rise.Shared.Common;
using Rise.Shared.Rates;

namespace Rise.Client.Rates;

public sealed class RateService(HttpClient httpClient) : IRateService
{
    public async Task<Result<RateResponse.Create>> CreateAsync(
        RateRequest.Create request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/rates", request, cancellationToken);

        return (await response.Content
            .ReadFromJsonAsync<Result<RateResponse.Create>>(
                cancellationToken: cancellationToken))!;
    }

    public async Task<Result<RateResponse.Index>> GetIndexAsync(
        RateRequest.Index request,
        CancellationToken ctx = default)
    {
        return (await httpClient.GetFromJsonAsync<Result<RateResponse.Index>>(
            $"/api/rates{request}",
            cancellationToken: ctx))!;
    }
}