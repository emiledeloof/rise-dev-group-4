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
    /// <summary>
    /// Returns everything about one booking for the admin detail page, including private guest data.
    /// </summary>
    Task<Result<BookingDto.AdminDetail>> GetDetailAsync(int bookingId, CancellationToken ctx = default);
    Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken ctx = default);
    /// <summary>
    /// Returns a paginated list of all bookings for the admin overview.
    /// </summary>
    Task<Result<BookingResponse.Index>> GetIndexAsync(BookingRequest.Index request, CancellationToken ctx = default);

    /// <summary>
    /// Returns all rooms with their bookings that overlap the given date window, for the admin calendar.
    /// </summary>
    Task<Result<BookingResponse.Matrix>> GetMatrixAsync(BookingRequest.Matrix request, CancellationToken ctx = default);

    /// <summary>
    /// Cancels a booking and records the refund decision, the booking stays visible to the admin.
    /// </summary>
    Task<Result<BookingResponse.Cancel>> CancelAsync(BookingRequest.Cancel request, CancellationToken ctx = default);
}