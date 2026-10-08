using Rise.Shared.Common;

namespace Rise.Services.Payments;

/// <summary>
/// Applies verified Stripe payment events to local payment records.
/// </summary>
public interface IStripeWebhookService
{
    Task<Result> HandleCheckoutSessionCompletedAsync(
        StripeCheckoutSessionCompleted payment,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifies a completed Stripe Checkout Session.
/// </summary>
public sealed record StripeCheckoutSessionCompleted(
    string SessionId,
    int PaymentId,
    DateTime PaidAt);
