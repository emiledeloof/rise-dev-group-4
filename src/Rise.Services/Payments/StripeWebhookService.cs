using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Services.Pricing;
using Rise.Shared.Common;
using Rise.Shared.Email;
using Stripe;

namespace Rise.Services.Payments;

/// <summary>
/// Updates local payments after Stripe has verified a completed Checkout Session
/// and sends a booking confirmation email to the guest.
/// </summary>
public sealed class StripeWebhookService(
    ApplicationDbContext dbContext,
    IStripePaymentGateway paymentGateway,
    IEmailService emailService) : IStripeWebhookService
{
    public async Task<Result> HandleCheckoutSessionCompletedAsync(
        StripeCheckoutSessionCompleted payment,
        CancellationToken cancellationToken = default)
    {
        var localPayment = await dbContext.Payments
            .Include(x => x.Booking)
                .ThenInclude(b => b.Guest)
            .Include(x => x.Booking)
                .ThenInclude(b => b.Rooms)
                    .ThenInclude(br => br.Room)
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

        // ! the guest finished an open checkout after the admin cancelled, the money is real so it stays paid,
        // the admin detail page shows a warning, the refund is done by hand
        if (localPayment.Booking.IsCancelled)
            Log.Warning("Payment {PaymentId} was paid after booking {BookingNumber} was cancelled, refund it by hand.",
                localPayment.Id, localPayment.Booking.BookingNumber);

        await SendConfirmationEmailAsync(localPayment.Booking, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Builds and sends the booking confirmation email to the guest.
    /// Email failures are caught and logged so a transient SMTP error never causes the
    /// webhook to report failure to Stripe (which would trigger a retry).
    /// </summary>
    private async Task SendConfirmationEmailAsync(Booking booking, CancellationToken ct)
    {
        try
        {
            var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;
            var email = new BookingConfirmationEmail
            {
                GuestName = booking.Guest.Name,
                GuestEmail = booking.Guest.Email,
                BookingNumber = booking.BookingNumber,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                Nights = nights,
                Adults = booking.Adults,
                TotalAmount = booking.Rooms.Sum(r => PriceCalculator.Total(r.PricePerNight, nights)),
                Rooms = booking.Rooms
                    .Select(r => new BookingConfirmationRoom
                    {
                        RoomNumber = r.Room.RoomNumber,
                        RoomName = r.Room.Name,
                        RateName = r.RateName,
                        PricePerNight = r.PricePerNight,
                        Total = PriceCalculator.Total(r.PricePerNight, nights),
                    })
                    .ToList(),
            };

            await emailService.SendBookingConfirmationAsync(email, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to send confirmation email for booking {BookingNumber}. The payment is still marked as paid.",
                booking.BookingNumber);
        }
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
