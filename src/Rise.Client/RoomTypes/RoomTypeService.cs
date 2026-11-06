using System.Net.Http.Json;
using Rise.Client.Common;
using Rise.Shared.RoomTypes;

namespace Rise.Client.RoomTypes;

/// <summary>client side of IRoomTypeService, calls the room type endpoints over http</summary>
public class RoomTypeService(HttpClient httpClient) : IRoomTypeService
{
    private const string Url = "/api/room-types";

    /// <summary>get on the room types url</summary>
    public async Task<Result<RoomTypeResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var response = await httpClient.GetAsync(Url, ctx);
        return await ResultReader.ReadAsync<RoomTypeResponse.Index>(response, ctx);
    }

    /// <summary>post with the new type in the body</summary>
    public async Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(Url, request, ctx);
        return await ResultReader.ReadAsync<RoomTypeResponse.Create>(response, ctx);
    }

    /// <summary>put with the changed type in the body</summary>
    public async Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default)
    {
        var response = await httpClient.PutAsJsonAsync(Url, request, ctx);
        return await ResultReader.ReadAsync(response, ctx);
    }

    /// <summary>delete with the id of the type in the url</summary>
    public async Task<Result> DeleteAsync(int roomTypeId, CancellationToken ctx = default)
    {
        var response = await httpClient.DeleteAsync($"{Url}/{roomTypeId}", ctx);
        return await ResultReader.ReadAsync(response, ctx);
    }
}
