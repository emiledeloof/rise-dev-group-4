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

        // ! the guest can finish an open checkout created before the admin cancelled rooms, then it charges the old amount
        // a checkout created after the cancellation only charges the remaining rooms, so compare the amount instead of the cancellation
        // the money is real so it stays paid, the admin detail page shows a warning, the refund is done by hand
        var booking = localPayment.Booking;
        var overpaid = localPayment.Amount - booking.ActiveAmount;
        if (overpaid > 0)
            Log.Warning("Payment {PaymentId} of booking {BookingNumber} is {Overpaid} more than the remaining rooms cost, refund the difference by hand.",
                localPayment.Id, booking.BookingNumber, overpaid);

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
