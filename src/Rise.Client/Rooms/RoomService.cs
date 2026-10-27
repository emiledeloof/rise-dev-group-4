using System.Net.Http.Json;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Client.Rooms;

/// <summary>
/// client side of IRoomService, calls the room endpoints of the server
/// same idea as the client InvoiceService
/// </summary>
public class RoomService(HttpClient httpClient) : IRoomService
{
    /// <summary>
    /// asks the server for the rooms free during the stay
    /// </summary>
    public async Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        // dates and guests in the url, same format as the search page uses, eg ?checkIn=2026-10-10&checkOut=2026-10-12&adults=2
        var url = $"/api/rooms/available?checkIn={request.CheckIn:yyyy-MM-dd}&checkOut={request.CheckOut:yyyy-MM-dd}&adults={request.Adults}";

        // GET on the endpoint and read the json answer as a Result
        return (await httpClient.GetFromJsonAsync<Result<RoomResponse.Available>>(url, cancellationToken: ctx))!;
    }

    /// <summary>
    /// asks the server for one room, for the room detail page
    /// </summary>
    public async Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        // GetAsync and not GetFromJsonAsync, an unknown room comes back as a 404
        // GetFromJsonAsync would throw on it, but the body still holds the NotFound Result we want
        var response = await httpClient.GetAsync($"/api/rooms/{roomId}", ctx);

        // any other error, eg a 503 while the server starts, has no Result body, so the page shows its error msg instead of crashing
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<Result<RoomDto.Detail>>(cancellationToken: ctx))!;
    }

    /// <summary>
    /// asks the server for the first rooms, for the landing page
    /// </summary>
    public async Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default)
    {
        // max nb of rooms in the url, eg /api/rooms?take=3
        var response = await httpClient.GetAsync($"/api/rooms{request}", ctx);

        // same as GetDetailAsync, an error without a Result body becomes an error Result so the page shows its msg
        if (!response.IsSuccessStatusCode)
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<Result<RoomResponse.List>>(cancellationToken: ctx))!;
    }
}
