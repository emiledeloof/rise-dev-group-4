namespace Rise.Shared.Bookings;

/// <summary>
/// Contains requests for booking payment operations.
/// </summary>
public static partial class BookingRequest
{
    /// <summary>
    /// Requests a Stripe Checkout Session for an existing booking.
    /// </summary>
    public sealed class CreateCheckoutSession
    {
        /// <summary>
        /// Gets or sets the booking identifier.
        /// </summary>
        public int BookingId { get; set; }
    }
}

/// <summary>
/// Contains responses for booking payment operations.
/// </summary>
public static partial class BookingResponse
{
    /// <summary>
    /// Contains the URL and identifier of a Stripe Checkout Session.
    /// </summary>
    public sealed class CheckoutSession
    {
        /// <summary>
        /// Gets or sets the URL where the customer should complete payment.
        /// </summary>
        public required string CheckoutUrl { get; init; }

        /// <summary>
        /// Gets or sets the Stripe Checkout Session identifier.
        /// </summary>
        public required string SessionId { get; init; }
    }
}
