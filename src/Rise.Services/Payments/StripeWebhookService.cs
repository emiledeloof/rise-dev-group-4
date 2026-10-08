using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Common;

namespace Rise.Services.Payments;

/// <summary>
/// Updates local payments after Stripe has verified a completed Checkout Session.
/// </summary>
public sealed class StripeWebhookService(ApplicationDbContext dbContext) : IStripeWebhookService
{
    public async Task<Result> HandleCheckoutSessionCompletedAsync(
        StripeCheckoutSessionCompleted payment,
        CancellationToken cancellationToken = default)
    {
        var localPayment = await dbContext.Payments
            .SingleOrDefaultAsync(x => x.Id == payment.PaymentId, cancellationToken);

        if (localPayment is null)
            return Result.NotFound($"Payment with Id '{payment.PaymentId}' was not found.");

        if (!string.Equals(localPayment.StripeCheckoutSession, payment.SessionId, StringComparison.Ordinal))
            return Result.Invalid(new ValidationError("The Stripe Checkout Session does not match the payment."));

        if (localPayment.IsPaid)
            return Result.Success();

        localPayment.MarkAsPaid(payment.PaidAt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
