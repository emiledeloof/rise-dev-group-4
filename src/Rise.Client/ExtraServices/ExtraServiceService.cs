using System.Net.Http.Json;
using Rise.Shared.ExtraServices;

namespace Rise.Client.ExtraServices;

/// <summary>
/// Client-side implementation of IExtraServiceService invoking API endpoints via HttpClient.
/// </summary>
public class ExtraServiceService(HttpClient httpClient) : IExtraServiceService
{
    private const string Url = "/api/extra-services";

    public async Task<Result<ExtraServiceResponse.Index>> GetIndexAsync(bool onlyActive = false, CancellationToken ctx = default)
    {
        var endpoint = onlyActive ? $"{Url}?onlyActive=true" : Url;
        var response = await httpClient.GetAsync(endpoint, ctx);
        return await ReadAsync<Result<ExtraServiceResponse.Index>>(response, ctx) ?? FailedRequest<ExtraServiceResponse.Index>(response);
    }

    public async Task<Result<ExtraServiceDto.Index>> GetByIdAsync(int id, CancellationToken ctx = default)
    {
        var response = await httpClient.GetAsync($"{Url}/{id}", ctx);
        return await ReadAsync<Result<ExtraServiceDto.Index>>(response, ctx) ?? FailedRequest<ExtraServiceDto.Index>(response);
    }

    public async Task<Result<ExtraServiceResponse.Create>> CreateAsync(ExtraServiceRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(Url, request, ctx);
        return await ReadAsync<Result<ExtraServiceResponse.Create>>(response, ctx) ?? FailedRequest<ExtraServiceResponse.Create>(response);
    }

    public async Task<Result> EditAsync(ExtraServiceRequest.Edit request, CancellationToken ctx = default)
    {
        var response = await httpClient.PutAsJsonAsync(Url, request, ctx);
        return await ReadAsync<Result>(response, ctx) ?? FailedRequest(response);
    }

    public async Task<Result> ToggleActiveAsync(int id, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsync($"{Url}/{id}/toggle-active", null, ctx);
        return await ReadAsync<Result>(response, ctx) ?? FailedRequest(response);
    }

    private static async Task<TResult?> ReadAsync<TResult>(HttpResponseMessage response, CancellationToken ctx)
        where TResult : class, Ardalis.Result.IResult
    {
        try
        {
            var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: ctx);
            var statusMatches = result is not null && (result.Status == ResultStatus.Ok) == response.IsSuccessStatusCode;
            return statusMatches ? result : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static Result FailedRequest(HttpResponseMessage response)
    {
        Log.Warning("Extra service request failed with http status {StatusCode}", (int)response.StatusCode);
        return Result.Error("The server could not handle the request, please try again.");
    }

    private static Result<T> FailedRequest<T>(HttpResponseMessage response)
    {
        Log.Warning("Extra service request failed with http status {StatusCode}", (int)response.StatusCode);
        return Result<T>.Error("The server could not handle the request, please try again.");
    }
}
