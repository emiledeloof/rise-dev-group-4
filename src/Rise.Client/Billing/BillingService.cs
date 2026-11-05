using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Billing;

namespace Rise.Client.Billing;

public class BillingService(HttpClient httpClient) : IBillingService
{
    public async Task<Result<BillingDto.Bill>> GetBillAsync(int bookingId, CancellationToken ctx = default)
    {
        // GetAsync and not GetFromJsonAsync, an unknown booking comes back as a 404 that still holds the NotFound Result
        var response = await httpClient.GetAsync($"/api/bookings/{bookingId}/bill", ctx);
        return await ReadAsync(response, ctx);
    }

    public async Task<Result<BillingDto.Bill>> AddLineAsync(BillingRequest.AddLine request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/bookings/{request.BookingId}/bill/lines", request, ctx);
        return await ReadAsync(response, ctx);
    }

    public async Task<Result<BillingDto.Bill>> EditLineAsync(BillingRequest.EditLine request, CancellationToken ctx = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/bookings/{request.BookingId}/bill/lines/{request.LineId}", request, ctx);
        return await ReadAsync(response, ctx);
    }

    public async Task<Result<BillingDto.Bill>> DeleteLineAsync(BillingRequest.DeleteLine request, CancellationToken ctx = default)
    {
        var response = await httpClient.DeleteAsync($"/api/bookings/{request.BookingId}/bill/lines/{request.LineId}", ctx);
        return await ReadAsync(response, ctx);
    }

    /// <summary>
    /// same rules as BookingService.CancelAsync, only these codes hold a Result body, a 401 or a 503 has none
    /// </summary>
    private static async Task<Result<BillingDto.Bill>> ReadAsync(HttpResponseMessage response, CancellationToken ctx)
    {
        var result = response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest
            or HttpStatusCode.NotFound or HttpStatusCode.Conflict
            ? await response.Content.ReadFromJsonAsync<Result<BillingDto.Bill>>(cancellationToken: ctx)
            : null;

        // a FastEndpoints validation error is a 400 without a Result, it would be read as a success
        if (result is null || result.IsSuccess != response.IsSuccessStatusCode)
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return result;
    }
}
