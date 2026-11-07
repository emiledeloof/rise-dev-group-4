using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Client.Rooms;

/// <summary>
/// client side of IRoomService, calls the room endpoints of the server
/// sends the language picked on the site, so the server answers with the texts of the guest
/// </summary>
public class RoomService(HttpClient httpClient, Toolbelt.Blazor.I18nText.I18nText i18nText) : IRoomService
{
    /// <summary>
    /// asks the server for the rooms free during the stay
    /// </summary>
    public Task<Result<RoomResponse.Available>> GetAvailableAsync(SearchRequest.StayPeriod request, CancellationToken ctx = default)
    {
        // dates, guests and rooms in the url, same format as the search page uses, eg ?checkIn=2026-10-10&checkOut=2026-10-12&adults=2&rooms=1
        return GetAsync<RoomResponse.Available>(
            $"/api/rooms/available?checkIn={request.CheckIn:yyyy-MM-dd}&checkOut={request.CheckOut:yyyy-MM-dd}&adults={request.Adults}&rooms={request.Rooms}", ctx);
    }

    /// <summary>
    /// asks the server for one room, for the room detail page
    /// </summary>
    public Task<Result<RoomDto.Detail>> GetDetailAsync(int roomId, CancellationToken ctx = default)
    {
        return GetAsync<RoomDto.Detail>($"/api/rooms/{roomId}", ctx);
    }

    /// <summary>
    /// asks the server for the first rooms, for the landing page
    /// </summary>
    public Task<Result<RoomResponse.List>> GetListAsync(RoomRequest.List request, CancellationToken ctx = default)
    {
        // max nb of rooms in the url, eg /api/rooms?take=3
        return GetAsync<RoomResponse.List>($"/api/rooms{request}", ctx);
    }

    /// <summary>
    /// GET on the url in the language of the guest and reads the answer as a Result
    /// GetAsync and not GetFromJsonAsync, a 400 or 404 would make GetFromJsonAsync throw but its body holds the Result we want
    /// </summary>
    private async Task<Result<T>> GetAsync<T>(string url, CancellationToken ctx)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // ! set here and not in a message handler, the handlers live in an other di scope with an other I18nText
        request.Headers.AcceptLanguage.ParseAdd(await i18nText.GetCurrentLanguageAsync());
        var response = await httpClient.SendAsync(request, ctx);

        // any other error, eg a 503 while the server starts, has no Result body, so the page shows its error msg instead of crashing
        if (!response.IsSuccessStatusCode && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.NotFound))
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<Result<T>>(cancellationToken: ctx))!;
    }
}
