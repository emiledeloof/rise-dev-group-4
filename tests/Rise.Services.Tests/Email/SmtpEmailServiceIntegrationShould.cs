using Microsoft.Extensions.Configuration;
using Rise.Services.Email;
using Rise.Shared.Email;

namespace Rise.Services.Tests.Email;

public class SmtpEmailServiceIntegrationShould
{
    [Fact]
    public async Task SendRealEmailToMailpitIfRunning()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:SmtpHost"] = "localhost",
                ["Email:SmtpPort"] = "1025",
                ["Email:UseSsl"] = "false",
                ["Email:FromAddress"] = "reservations@rise-hotel.example.com",
                ["Email:FromName"] = "Rise Hotel",
            })
            .Build();

        var service = new SmtpEmailService(config);

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
