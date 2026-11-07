using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

public class BookingService(HttpClient httpClient) : IBookingService
{
    public async Task<Result<BookingDto.AdminDetail>> GetDetailAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        // GetAsync and not GetFromJsonAsync, an unknown booking comes back as a 404 that still holds the NotFound Result
        var response = await httpClient.GetAsync($"/api/bookings/{bookingId}", cancellationToken);

        // any other error, eg a 401 or a 503, has no Result body, so the page shows its error msg instead of crashing
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<Result<BookingDto.AdminDetail>>(
            cancellationToken: cancellationToken))!;
    }

    public async Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        var encodedSessionId = Uri.EscapeDataString(checkoutSessionId);
        return (await httpClient.GetFromJsonAsync<Result<BookingDto.Detail>>(
            $"/api/bookings/checkout-session/{encodedSessionId}",
            cancellationToken))!;
    }

    public async Task<Result<BookingResponse.Create>> CreateAsync(
        BookingRequest.Create request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/bookings", request, cancellationToken);

        return (await response.Content
            .ReadFromJsonAsync<Result<BookingResponse.Create>>(
                cancellationToken: cancellationToken))!;
    }

    public async Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(
        BookingRequest.CreateCheckoutSession request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/bookings/{request.BookingId}/checkout-session",
            request,
            cancellationToken);

        return (await response.Content
            .ReadFromJsonAsync<Result<BookingResponse.CheckoutSession>>(
                cancellationToken: cancellationToken))!;
    }

    public async Task<Result<BookingResponse.Index>> GetIndexAsync(
        BookingRequest.Index request,
        CancellationToken cancellationToken = default)
    {
        return (await httpClient.GetFromJsonAsync<Result<BookingResponse.Index>>(
            $"/api/bookings{request}",
            cancellationToken))!;
    }

    public async Task<Result<BookingResponse.Cancel>> CancelAsync(
        BookingRequest.Cancel request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/bookings/{request.BookingId}/cancel", request, cancellationToken);

        return await ReadCommandResultAsync<BookingResponse.Cancel>(response, cancellationToken);
    }

    public async Task<Result<BookingResponse.Refund>> RecordRefundAsync(
        BookingRequest.RecordRefund request,
        CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/bookings/{request.BookingId}/rooms/{request.BookingRoomId}/refund", request, ctx);

        return await ReadCommandResultAsync<BookingResponse.Refund>(response, ctx);
    }

    public async Task<Result<BookingResponse.Refund>> UndoRefundAsync(
        BookingRequest.UndoRefund request,
        CancellationToken ctx = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/api/bookings/{request.BookingId}/rooms/{request.BookingRoomId}/refund", ctx);

        return await ReadCommandResultAsync<BookingResponse.Refund>(response, ctx);
    }

    /// <summary>
    /// reads the Result of a command that changes a booking, any answer without a Result becomes an error
    /// </summary>
    private static async Task<Result<T>> ReadCommandResultAsync<T>(HttpResponseMessage response, CancellationToken ctx)
    {
        // only these codes hold a Result body, a 401 or a 503 has none
        var result = response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest
            or HttpStatusCode.NotFound or HttpStatusCode.Conflict
            ? await response.Content.ReadFromJsonAsync<Result<T>>(cancellationToken: ctx)
            : null;

        // a FastEndpoints validation error is a 400 without a Result, it would be read as a success
        if (result is null || result.IsSuccess != response.IsSuccessStatusCode)
            return Result.Error($"The server answered {(int)response.StatusCode}.");

        return result;
    }

    public async Task<Result<BookingResponse.Matrix>> GetMatrixAsync(
        BookingRequest.Matrix request,
        CancellationToken cancellationToken = default)
    {
        var qs = $"?windowStart={request.WindowStart:yyyy-MM-dd}&windowDays={request.WindowDays}";
        return (await httpClient.GetFromJsonAsync<Result<BookingResponse.Matrix>>(
            $"/api/bookings/matrix{qs}",
            cancellationToken))!;
    }
}
