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
        // dates in the url as yyyy-MM-dd, same format as the search page uses, eg ?checkIn=2026-10-10&checkOut=2026-10-12
        var url = $"/api/rooms/available?checkIn={request.CheckIn:yyyy-MM-dd}&checkOut={request.CheckOut:yyyy-MM-dd}";

        // GET on the endpoint and read the json answer as a Result
        return (await httpClient.GetFromJsonAsync<Result<RoomResponse.Available>>(url, cancellationToken: ctx))!;
    }
}
