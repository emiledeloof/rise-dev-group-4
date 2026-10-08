using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Rise.Services.Payments;

/// <summary>
/// Stripe.NET implementation of the payment gateway.
/// </summary>
public sealed class StripePaymentGateway : IStripePaymentGateway
{
    private readonly StripeClient stripeClient;
    private readonly IConfiguration configuration;

    public StripePaymentGateway(IConfiguration configuration)
    {
        this.configuration = configuration;
        var secretKey = configuration["Stripe:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Stripe:SecretKey is not configured.");

        stripeClient = new StripeClient(secretKey);
    }

    public async Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = configuration["Stripe:SuccessUrl"]
                        ?? throw new InvalidOperationException("Stripe:SuccessUrl is not configured."),
            CancelUrl = configuration["Stripe:CancelUrl"]
                       ?? throw new InvalidOperationException("Stripe:CancelUrl is not configured."),
            CustomerEmail = request.CustomerEmail,
            Metadata = new Dictionary<string, string>
            {
                ["BookingId"] = request.BookingId,
                ["PaymentId"] = request.PaymentId,
                ["BookingNumber"] = request.BookingNumber
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = decimal.ToInt64(request.Amount * 100m),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Booking {request.BookingNumber}",
                            Description = "B&B booking"
                        }
                    }
                }
            ]
        };

        var service = stripeClient.V1.Checkout.Sessions;
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            throw new InvalidOperationException("Stripe returned an incomplete Checkout Session.");

        return new StripeCheckoutSession(session.Id, session.Url);
    }
}
