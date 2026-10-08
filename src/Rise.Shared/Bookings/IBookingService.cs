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
    Task<Result<BookingResponse.Create>> CreateAsync(BookingRequest.Create request, CancellationToken ctx = default);
    Task<Result<BookingDto.Detail>> GetDetailAsync(int bookingId, CancellationToken ctx = default);
}
