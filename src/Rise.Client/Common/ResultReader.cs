using System.Net.Http.Json;

namespace Rise.Client.Common;

/// <summary>reads the result sent by the api, shared by the client services</summary>
public static class ResultReader
{
    private const string FailedMessage = "The server could not handle the request, please try again.";

    /// <summary>the result of the body, or an error result when the body is not a result that matches the http status</summary>
    public static async Task<Result<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken ctx = default)
        => await TryReadAsync<Result<T>>(response, ctx) ?? Failed(response);

    /// <summary>same as the generic version, for an endpoint that returns a result without value</summary>
    public static async Task<Result> ReadAsync(HttpResponseMessage response, CancellationToken ctx = default)
        => await TryReadAsync<Result>(response, ctx) ?? Failed(response);

    // ! a 401, a 500 with an empty body or a 400 of fastendpoints has an other body, read as a result it would look like a success
    private static async Task<TResult?> TryReadAsync<TResult>(HttpResponseMessage response, CancellationToken ctx)
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

    private static Result Failed(HttpResponseMessage response)
    {
        Log.Warning("Request failed with http status {StatusCode}", (int)response.StatusCode);
        return Result.Error(FailedMessage);
    }
}
