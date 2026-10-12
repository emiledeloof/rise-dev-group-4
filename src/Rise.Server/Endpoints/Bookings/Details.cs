using Rise.Shared.Bookings;

namespace Rise.Server.Endpoints.Bookings;

public class Detail(IBookingService bookingService)
    : EndpointWithoutRequest<Result<BookingDto.Detail>>
{
    public override void Configure()
    {
        Get("/api/bookings/{bookingId}");
        // Choose authorization appropriate to who may view a booking.
    }

    public override Task<Result<BookingDto.Detail>> ExecuteAsync(CancellationToken ct)
    {
        var bookingId = Route<int>("bookingId");
        return bookingService.GetDetailAsync(bookingId, ct);
    }
}
public sealed class DetailByCheckoutSession(IBookingService bookingService)
    : EndpointWithoutRequest<Result<BookingDto.Detail>>
{
    public override void Configure()
    {
        Get("/api/bookings/checkout-session/{checkoutSessionId}");
        AllowAnonymous();
    }

    public override Task<Result<BookingDto.Detail>> ExecuteAsync(CancellationToken ct)
    {
        var checkoutSessionId = Route<string>("checkoutSessionId") ?? string.Empty;
        return bookingService.GetDetailByCheckoutSessionAsync(checkoutSessionId, ct);
    }
}