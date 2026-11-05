using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Common;
using Stripe;

namespace Rise.Services.Payments;

/// <summary>
/// Updates local payments after Stripe has verified a completed Checkout Session.
/// </summary>
public sealed class StripeWebhookService(
    ApplicationDbContext dbContext,
    IStripePaymentGateway paymentGateway) : IStripeWebhookService
{
    public async Task<Result> HandleCheckoutSessionCompletedAsync(
        StripeCheckoutSessionCompleted payment,
        CancellationToken cancellationToken = default)
    {
        var localPayment = await dbContext.Payments
            .Include(x => x.Booking)
                .ThenInclude(x => x.Rooms)
            .SingleOrDefaultAsync(x => x.Id == payment.PaymentId, cancellationToken);

        if (localPayment is null)
            return Result.NotFound($"Payment with Id '{payment.PaymentId}' was not found.");

        if (!string.Equals(localPayment.StripeCheckoutSession, payment.SessionId, StringComparison.Ordinal))
            return Result.Invalid(new ValidationError("The Stripe Checkout Session does not match the payment."));

        if (localPayment.IsPaid)
            return Result.Success();

        var details = await TryGetPaymentDetailsAsync(payment.SessionId, cancellationToken);

        localPayment.MarkAsPaid(details?.PaidAt ?? payment.PaidAt);
        if (details is not null)
            localPayment.SetStripePaymentDetails(details.PaymentIntentId, details.ReceiptUrl, details.CardBrand, details.CardLast4);

        await dbContext.SaveChangesAsync(cancellationToken);

        // ! the guest finished an open checkout after the admin cancelled some or all rooms, the amount can be too high
        // the money is real so it stays paid, the admin detail page shows a warning, the refund is done by hand
        if (localPayment.Booking.IsCancelled || localPayment.Booking.IsPartiallyCancelled)
            Log.Warning("Payment {PaymentId} was paid after rooms of booking {BookingNumber} were cancelled, refund the difference by hand.",
                localPayment.Id, localPayment.Booking.BookingNumber);

        return Result.Success();
    }

    /// <summary>
    /// The verified webhook already proves the payment, so the extra Stripe details are optional.
    /// </summary>
    private async Task<StripePaymentDetails?> TryGetPaymentDetailsAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            return await paymentGateway.GetPaymentDetailsAsync(sessionId, cancellationToken);
        }
        catch (StripeException exception)
        {
            Log.Warning(exception, "Could not load the Stripe payment details for Checkout Session {SessionId}.", sessionId);
            return null;
        }
    }
}
