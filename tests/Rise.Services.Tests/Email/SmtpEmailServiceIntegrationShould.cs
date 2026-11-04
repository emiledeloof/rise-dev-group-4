using Microsoft.Extensions.Options;
using Rise.Services.Email;
using Rise.Shared.Email;

namespace Rise.Services.Tests.Email;

public class SmtpEmailServiceIntegrationShould
{
    [Fact]
    public async Task SendRealEmailToMailpitIfRunning()
    {
        var host = Environment.GetEnvironmentVariable("MAILPIT_HOST");
        if (string.IsNullOrWhiteSpace(host))
            return; // Mailpit not available – skip silently on CI

        var options = Options.Create(new EmailOptions
        {
            SmtpHost = host,
            SmtpPort = 1025,
            UseSsl = false,
            FromAddress = "reservations@rise-hotel.example.com",
            FromName = "Rise Hotel",
        });

        var service = new SmtpEmailService(options);

        var email = new BookingConfirmationEmail
        {
            BookingNumber = "20261030-SVM-999999",
            GuestName = "Maria Silva",
            GuestEmail = "MariaSilva@gmail.com",
            CheckIn = new DateOnly(2026, 11, 1),
            CheckOut = new DateOnly(2026, 11, 4),
            Nights = 3,
            Adults = 2,
            TotalAmount = 327.00m,
            Rooms = new List<BookingConfirmationRoom>
            {
                new()
                {
                    RoomNumber = "102",
                    RoomName = "Double Room Deluxe",
                    RateName = "Flexible Rate",
                    PricePerNight = 109.00m,
                    Total = 327.00m
                }
            }
        };

        await service.SendBookingConfirmationAsync(email);
    }
}
