using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using Rise.Shared.Email;

namespace Rise.Services.Email;

/// <summary>
/// Sends transactional emails via SMTP.
/// Connection details are read from <see cref="EmailOptions"/> (bound from the <c>Email</c> configuration section).
/// </summary>
public sealed class SmtpEmailService(IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendBookingConfirmationAsync(BookingConfirmationEmail email, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = !string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password)
                ? new NetworkCredential(_options.Username, _options.Password)
                : null,
        };

        var body = BuildHtmlBody(email);
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = $"Booking Confirmation – {email.BookingNumber}",
            Body = body,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(email.GuestEmail, email.GuestName));

        await client.SendMailAsync(message, ct);

        Log.Information(
            "Confirmation email sent for booking {BookingNumber} to {GuestEmail}.",
            email.BookingNumber, email.GuestEmail);
    }

    private static string BuildHtmlBody(BookingConfirmationEmail email)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'><head><meta charset='UTF-8'></head><body style='font-family:sans-serif;color:#333'>");
        sb.AppendLine($"<h2 style='color:#2c5f8a'>Booking Confirmed – {email.BookingNumber}</h2>");
        sb.AppendLine($"<p>Dear {email.GuestName},</p>");
        sb.AppendLine("<p>Thank you for your payment. Your reservation is confirmed. Here are your booking details:</p>");

        // Stay summary
        sb.AppendLine("<table style='border-collapse:collapse;margin-bottom:1rem'>");
        sb.AppendLine(Row("Check-in", email.CheckIn.ToString("D", CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Check-out", email.CheckOut.ToString("D", CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Nights", email.Nights.ToString(CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Guests", email.Adults.ToString(CultureInfo.InvariantCulture)));
        sb.AppendLine("</table>");

        // Room breakdown
        sb.AppendLine("<h3>Room Details</h3>");
        sb.AppendLine("<table style='border-collapse:collapse;width:100%;max-width:600px'>");
        sb.AppendLine("<thead><tr style='background:#2c5f8a;color:#fff'>");
        sb.AppendLine("<th style='padding:8px;text-align:left'>Room</th>");
        sb.AppendLine("<th style='padding:8px;text-align:left'>Rate</th>");
        sb.AppendLine("<th style='padding:8px;text-align:right'>Per night</th>");
        sb.AppendLine("<th style='padding:8px;text-align:right'>Total</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var room in email.Rooms)
        {
            sb.AppendLine($"<tr style='border-bottom:1px solid #ddd'>");
            sb.AppendLine($"<td style='padding:8px'>{room.RoomNumber} – {room.RoomName}</td>");
            sb.AppendLine($"<td style='padding:8px'>{room.RateName}</td>");
            sb.AppendLine($"<td style='padding:8px;text-align:right'>€{room.PricePerNight.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"<td style='padding:8px;text-align:right'>€{room.Total.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine($"<tr style='font-weight:bold;background:#f5f5f5'>");
        sb.AppendLine("<td colspan='3' style='padding:8px;text-align:right'>Total</td>");
        sb.AppendLine($"<td style='padding:8px;text-align:right'>€{email.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</td>");
        sb.AppendLine("</tr></tbody></table>");

        sb.AppendLine("<p style='margin-top:1.5rem'>We look forward to welcoming you!</p>");
        sb.AppendLine("<p>Kind regards,<br/><strong>The Rise Hotel Team</strong></p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>Two-column summary row with a label on the left and a value on the right.</summary>
    private static string Row(string label, string value)
        => $"<tr><th style='padding:6px 16px 6px 0;text-align:left'>{label}</th><td style='padding:6px 0'>{value}</td></tr>";
}
