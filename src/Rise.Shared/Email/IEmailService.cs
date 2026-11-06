namespace Rise.Shared.Email;

/// <summary>
/// Sends transactional emails to guests (e.g. booking confirmation).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a booking confirmation email to the guest after a successful payment.
    /// </summary>
    Task SendBookingConfirmationAsync(BookingConfirmationEmail email, CancellationToken ct = default);
}
