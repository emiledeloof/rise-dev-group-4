using Rise.Shared.Bookings;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Creates a Stripe Checkout Session for a booking.
/// </summary>
public sealed class CreateCheckoutSession(IBookingService bookingService)
    : Endpoint<BookingRequest.CreateCheckoutSession, Result<BookingResponse.CheckoutSession>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/checkout-session");
        AllowAnonymous();
    }

    public override Task<Result<BookingResponse.CheckoutSession>> ExecuteAsync(
        BookingRequest.CreateCheckoutSession request,
        CancellationToken cancellationToken)
    {
        return bookingService.CreateCheckoutSessionAsync(request, cancellationToken);
    }
}
