using System.Globalization;
using System.Text;
using static System.Net.WebUtility;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Rise.Shared.Email;

namespace Rise.Services.Email;

/// <summary>
/// Sends transactional emails via SMTP using MailKit.
/// Connection details are read from <see cref="EmailOptions"/> (bound from the <c>Email</c> configuration section).
/// Only registered when <see cref="EmailOptions.SmtpHost"/> is configured;
/// otherwise <see cref="NullEmailService"/> is used.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            throw new InvalidOperationException("Cannot initialize SmtpEmailService without a configured SmtpHost.");
    }

    public async Task SendBookingConfirmationAsync(BookingConfirmationEmail email, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(email.GuestName, email.GuestEmail));
        message.Subject = $"Booking Confirmation \u2013 {email.BookingNumber}";
        message.Body = new TextPart(TextFormat.Html) { Text = BuildHtmlBody(email) };

        using var client = new SmtpClient();

        // UseSsl = true  → port 465, SSL on connect
        // UseSsl = false → port 587, STARTTLS when available (default for most providers)
        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, socketOptions, ct);

        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);

        Log.Information(
            "Confirmation email sent for booking {BookingNumber} to {GuestEmail}.",
            email.BookingNumber, email.GuestEmail);
    }

    private static string BuildHtmlBody(BookingConfirmationEmail email)
    {
        // All user-sourced strings are HTML-encoded to prevent injection / malformed output.
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'><head><meta charset='UTF-8'></head><body style='font-family:sans-serif;color:#333'>");
        sb.AppendLine($"<h2 style='color:#2c5f8a'>Booking Confirmed \u2013 {H(email.BookingNumber)}</h2>");
        sb.AppendLine($"<p>Dear {H(email.GuestName)},</p>");
        sb.AppendLine("<p>Thank you for your payment. Your reservation is confirmed. Here are your booking details:</p>");

        // Stay summary — dates/numbers are safe without encoding
        sb.AppendLine("<table style='border-collapse:collapse;margin-bottom:1rem'>");
        sb.AppendLine(Row("Check-in", email.CheckIn.ToString("D", CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Check-out", email.CheckOut.ToString("D", CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Nights", email.Nights.ToString(CultureInfo.InvariantCulture)));
        sb.AppendLine(Row("Adults", email.Adults.ToString(CultureInfo.InvariantCulture)));
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
            sb.AppendLine("<tr style='border-bottom:1px solid #ddd'>");
            sb.AppendLine($"<td style='padding:8px'>{H(room.RoomNumber)} \u2013 {H(room.RoomName)}</td>");
            sb.AppendLine($"<td style='padding:8px'>{H(room.RateName)}</td>");
            sb.AppendLine($"<td style='padding:8px;text-align:right'>\u20ac{room.PricePerNight.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine($"<td style='padding:8px;text-align:right'>\u20ac{room.Total.ToString("F2", CultureInfo.InvariantCulture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr style='font-weight:bold;background:#f5f5f5'>");
        sb.AppendLine("<td colspan='3' style='padding:8px;text-align:right'>Total</td>");
        sb.AppendLine($"<td style='padding:8px;text-align:right'>\u20ac{email.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</td>");
        sb.AppendLine("</tr></tbody></table>");

        sb.AppendLine("<p style='margin-top:1.5rem'>We look forward to welcoming you!</p>");
        sb.AppendLine("<p>Kind regards,<br/><strong>The Rise Hotel Team</strong></p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>HTML-encodes a user-supplied string to prevent injection in the email body.</summary>
    private static string H(string value) => HtmlEncode(value);

    /// <summary>Two-column summary row with a label on the left and a value on the right.</summary>
    private static string Row(string label, string value)
        => $"<tr><th style='padding:6px 16px 6px 0;text-align:left'>{label}</th><td style='padding:6px 0'>{value}</td></tr>";
}
