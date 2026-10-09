namespace Rise.Services.Payments;

/// <summary>
/// Creates Stripe Checkout Sessions without exposing Stripe SDK details to application services.
/// </summary>
public interface IStripePaymentGateway
{
    Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the PaymentIntent and charge of a paid Checkout Session, or null when it has none.
    /// </summary>
    Task<StripePaymentDetails?> GetPaymentDetailsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Data required to create a Stripe Checkout Session.
/// </summary>
public sealed record StripeCheckoutSessionRequest(
    string BookingId,
    string PaymentId,
    string BookingNumber,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    string CustomerEmail);

/// <summary>
/// Stripe Checkout Session data needed by the application.
/// </summary>
public sealed record StripeCheckoutSession(string SessionId, string CheckoutUrl);

/// <summary>
/// Stripe payment data shown in the invoice list.
/// </summary>
public sealed record StripePaymentDetails(
    string PaymentIntentId,
    string? ReceiptUrl,
    string? CardBrand,
    string? CardLast4,
    DateTime? PaidAt);
