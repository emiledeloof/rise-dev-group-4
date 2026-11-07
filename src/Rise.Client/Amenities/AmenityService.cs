using System.Net.Http.Json;
using Rise.Client.Common;
using Rise.Shared.Amenities;

namespace Rise.Client.Amenities;

/// <summary>client side of IAmenityService, calls the amenity endpoints over http</summary>
public class AmenityService(HttpClient httpClient) : IAmenityService
{
    private const string Url = "/api/amenities";

    /// <summary>get on the amenities url</summary>
    public async Task<Result<AmenityResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var response = await httpClient.GetAsync(Url, ctx);
        return await ResultReader.ReadAsync<AmenityResponse.Index>(response, ctx);
    }

    /// <summary>post with the new amenity in the body</summary>
    public async Task<Result<AmenityResponse.Create>> CreateAsync(AmenityRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(Url, request, ctx);
        return await ResultReader.ReadAsync<AmenityResponse.Create>(response, ctx);
    }

    /// <summary>put with the changed amenity in the body</summary>
    public async Task<Result> EditAsync(AmenityRequest.Edit request, CancellationToken ctx = default)
    {
        var response = await httpClient.PutAsJsonAsync(Url, request, ctx);
        return await ResultReader.ReadAsync(response, ctx);
    }

    /// <summary>post on the activate url of the amenity</summary>
    public async Task<Result> ActivateAsync(int amenityId, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsync($"{Url}/{amenityId}/activate", null, ctx);
        return await ResultReader.ReadAsync(response, ctx);
    }

    /// <summary>post on the deactivate url of the amenity</summary>
    public async Task<Result> DeactivateAsync(int amenityId, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsync($"{Url}/{amenityId}/deactivate", null, ctx);
        return await ResultReader.ReadAsync(response, ctx);
    }
}
