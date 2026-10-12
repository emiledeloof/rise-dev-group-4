using System.Net.Http.Json;
using Rise.Shared.Guests;

namespace Rise.Client.Guests;

/// <summary>
/// Client-side implementation of <see cref="IGuestService"/> for Blazor WebAssembly.
/// Communicates with the backend FastEndpoints API over HTTP.
/// </summary>
/// <param name="httpClient">The typed HttpClient configured with the backend API base address.</param>
public class GuestService(HttpClient httpClient) : IGuestService
{
    /// <summary>
    /// Sends a POST request containing the guest registration payload to "/api/guests"
    /// and deserializes the JSON response into an Ardalis <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="request">The guest registration data entered into the Blazor form.</param>
    /// <param name="ctx">Cancellation token for aborting the HTTP request if needed.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing <see cref="GuestResponse.Create"/> with the new Guest ID,
    /// or validation/conflict errors returned by the server.
    /// </returns>
    public async Task<Result<GuestResponse.Create>> CreateAsync(GuestRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/guests", request, ctx);
        var result = await response.Content.ReadFromJsonAsync<Result<GuestResponse.Create>>(cancellationToken: ctx);
        return result!;
    }
}
