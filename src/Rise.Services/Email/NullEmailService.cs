using Rise.Shared.Email;

namespace Rise.Services.Email;

/// <summary>
/// No-op email service used when SMTP is not configured (e.g. CI, local dev without Mailpit).
/// Every send is silently dropped and a warning is logged.
/// </summary>
internal sealed class NullEmailService : IEmailService
{
    public Task SendBookingConfirmationAsync(BookingConfirmationEmail email, CancellationToken ct = default)
    {
        Log.Warning(
            "Email sending is disabled (no SMTP host configured). " +
            "Booking confirmation for {BookingNumber} was not sent.",
            email.BookingNumber);
        return Task.CompletedTask;
    }
}
