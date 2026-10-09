using System.Net.Http.Json;
using Rise.Shared.Bookings;

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
}