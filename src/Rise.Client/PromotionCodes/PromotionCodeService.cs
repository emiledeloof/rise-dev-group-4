using System.Net.Http.Json;
using Rise.Shared.PromotionCodes;

namespace Rise.Client.PromotionCodes;

public class PromotionCodeService(HttpClient httpClient) : IPromotionCodeService
{
    public async Task<Result<PromotionCodeResponse.Create>> CreateAsync(
        PromotionCodeRequest.Create request,
        CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/promotion-codes", request, ctx);

        try
        {
            var result = await response.Content.ReadFromJsonAsync<Result<PromotionCodeResponse.Create>>(
                cancellationToken: ctx);
            if (result is not null && (result.Status == ResultStatus.Ok) == response.IsSuccessStatusCode)
                return result;
        }
        catch (System.Text.Json.JsonException)
        {
        }

        Log.Warning("Promotion code request failed with http status {StatusCode}", (int)response.StatusCode);
        return Result.Error("The server could not save the promotion code, please try again.");
    }
}
