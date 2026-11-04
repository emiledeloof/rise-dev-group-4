using System.Net.Http.Json;
using Rise.Shared.RoomTypes;

namespace Rise.Client.RoomTypes;

/// <summary>
/// client side of IRoomTypeService, calls the room type endpoints over http
/// same idea as RatePricing/RatePricingService.cs
/// </summary>
public class RoomTypeService(HttpClient httpClient) : IRoomTypeService
{
    private const string Url = "/api/room-types";

    /// <summary>
    /// get on the room types url
    /// </summary>
    public async Task<Result<RoomTypeResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var response = await httpClient.GetAsync(Url, ctx);
        return await ReadAsync<Result<RoomTypeResponse.Index>>(response, ctx) ?? FailedRequest(response);
    }

    /// <summary>
    /// post with the new type in the body
    /// </summary>
    public async Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(Url, request, ctx);
        return await ReadAsync<Result<RoomTypeResponse.Create>>(response, ctx) ?? FailedRequest(response);
    }

    /// <summary>
    /// put with the changed type in the body
    /// </summary>
    public async Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default)
    {
        var response = await httpClient.PutAsJsonAsync(Url, request, ctx);
        return await ReadAsync<Result>(response, ctx) ?? FailedRequest(response);
    }

    /// <summary>
    /// delete with the id of the type in the url
    /// </summary>
    public async Task<Result> DeleteAsync(int roomTypeId, CancellationToken ctx = default)
    {
        var response = await httpClient.DeleteAsync($"{Url}/{roomTypeId}", ctx);
        return await ReadAsync<Result>(response, ctx) ?? FailedRequest(response);
    }

    /// <summary>
    /// the result sent by the api, null when the body is not a result that matches the http status
    /// </summary>
    // ! a 401 or a 400 of fastendpoints has an other body, read as a result it would look like a success
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

    /// <summary>
    /// error result when the api did not answer with a usable result
    /// </summary>
    private static Result FailedRequest(HttpResponseMessage response)
    {
        Log.Warning("Room type request failed with http status {StatusCode}", (int)response.StatusCode);
        return Result.Error("The server could not handle the request, please try again.");
    }
}
