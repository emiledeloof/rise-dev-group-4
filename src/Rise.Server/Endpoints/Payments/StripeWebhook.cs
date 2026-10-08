using Stripe;
using Stripe.Checkout;
using Rise.Services.Payments;

namespace Rise.Server.Endpoints.Payments;

/// <summary>
/// Receives verified Stripe Checkout webhook events.
/// </summary>
public sealed class StripeWebhook(
    IConfiguration configuration,
    IStripeWebhookService webhookService,
    ILogger<StripeWebhook> logger)
    : EndpointWithoutRequest<Result>
{
    public override void Configure()
    {
        Post("/api/payments/stripe-webhook");
        AllowAnonymous();
    }

    public override async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"]?.Trim();
        if (string.IsNullOrWhiteSpace(webhookSecret))
            throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");

        var signature = HttpContext.Request.Headers["Stripe-Signature"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(signature))
            return Result.Invalid(new ValidationError("The Stripe-Signature header is required."));

        using var reader = new StreamReader(HttpContext.Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            logger.LogWarning(
                exception,
                "Stripe webhook signature verification failed. PayloadLength: {PayloadLength}, SignaturePresent: {SignaturePresent}",
                payload.Length,
                !string.IsNullOrWhiteSpace(signature));
            return Result.Invalid(new ValidationError("The Stripe webhook signature is invalid."));
        }

        if (stripeEvent.Type != "checkout.session.completed" &&
            stripeEvent.Type != "checkout.session.async_payment_succeeded")
            return Result.Success();

        if (stripeEvent.Data.Object is not Session session)
            return Result.Invalid(new ValidationError("The Stripe event does not contain a Checkout Session."));

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        if (!session.Metadata.TryGetValue("PaymentId", out var paymentIdValue) ||
            !int.TryParse(paymentIdValue, out var paymentId))
            return Result.Invalid(new ValidationError("The Checkout Session is missing a valid PaymentId."));

        var paidAt = session.Created == default ? DateTime.UtcNow : session.Created;
        return await webhookService.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(session.Id, paymentId, paidAt),
            cancellationToken);
    }
}
