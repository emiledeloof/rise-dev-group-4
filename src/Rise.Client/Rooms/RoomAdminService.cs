using System.Net.Http.Json;
using Rise.Client.Common;
using Rise.Shared.Rooms;

namespace Rise.Client.Rooms;

/// <summary>client side of IRoomAdminService, calls the admin room endpoints over http</summary>
public class RoomAdminService(HttpClient httpClient) : IRoomAdminService
{
    private const string Url = "/api/admin/rooms";

    /// <summary>get on the admin rooms url</summary>
    public async Task<Result<IReadOnlyList<RoomAdminDto.Index>>> GetIndexAsync(CancellationToken ctx = default)
    {
        var response = await httpClient.GetAsync(Url, ctx);
        return await ResultReader.ReadAsync<IReadOnlyList<RoomAdminDto.Index>>(response, ctx);
    }

    /// <summary>post with the new room in the body</summary>
    public async Task<Result<int>> CreateAsync(RoomRequest.Create request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(Url, request, ctx);
        // the body holds the result even for a 409, so the page can show the reason
        return await ResultReader.ReadAsync<int>(response, ctx);
    }
}
