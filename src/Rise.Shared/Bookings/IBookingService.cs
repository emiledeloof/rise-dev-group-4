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
    Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken ctx = default);
    /// <summary>
    /// Returns a paginated list of all bookings for the admin overview.
    /// </summary>
    Task<Result<BookingResponse.Index>> GetIndexAsync(QueryRequest.SkipTake request, CancellationToken ctx = default);
}
