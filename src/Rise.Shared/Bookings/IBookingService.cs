using Rise.Shared.Common;

namespace Rise.Shared.Bookings;

/// <summary>
/// Provides payment-related operations for bookings.
/// </summary>
public interface IBookingService
{
    Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(
        BookingRequest.CreateCheckoutSession request,
        CancellationToken cancellationToken = default);
}
