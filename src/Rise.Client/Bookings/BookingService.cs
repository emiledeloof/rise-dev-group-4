using System.Net.Http.Json;
using Rise.Shared.Bookings;
using Rise.Shared.Common;

namespace Rise.Client.Bookings;

public class BookingService(HttpClient httpClient) : IBookingService
{
    public async Task<Result<BookingDto.Detail>> GetDetailAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return (await httpClient.GetFromJsonAsync<Result<BookingDto.Detail>>(
            $"/api/bookings/{bookingId}",
            cancellationToken))!;
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
        QueryRequest.SkipTake request,
        CancellationToken cancellationToken = default)
    {
        return (await httpClient.GetFromJsonAsync<Result<BookingResponse.Index>>(
            $"/api/bookings{request}",
            cancellationToken))!;
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
