using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// admin booking details for the page tests
/// </summary>
public static class TestAdminDetails
{
    /// <summary>
    /// a paid booking of 2 rooms for 3 nights, with language and party filled in
    /// </summary>
    public static BookingDto.AdminDetail Full() => new()
    {
        Id = 7,
        BookingNumber = "20261004-SVM-000007",
        CreatedAt = new DateTime(2026, 10, 4, 9, 15, 0, DateTimeKind.Utc),
        Status = "Paid",
        CheckIn = new DateOnly(2026, 10, 17),
        CheckOut = new DateOnly(2026, 10, 20),
        Nights = 3,
        TotalAmount = 720m,
        Guest = new BookingDto.AdminGuestDto
        {
            Id = 3,
            Name = "Lukas Müller",
            Email = "lukas.mueller@example.com",
            Phone = "+49 151 23456789",
            AddressLine1 = "Hauptstraße 5",
            AddressLine2 = "2. Stock",
            Country = "Germany",
        },
        Rooms =
        [
            new() { RoomId = 1, RoomNumber = "101", RoomName = "Standard Room", RateName = "Standard", PricePerNight = 100m, Total = 300m },
            new() { RoomId = 3, RoomNumber = "103", RoomName = "Deluxe Room", RateName = "Flexible", PricePerNight = 140m, Total = 420m },
        ],
        Payments =
        [
            new()
            {
                Id = 11,
                Amount = 720m,
                PaidAt = new DateTime(2026, 10, 4, 9, 20, 0, DateTimeKind.Utc),
                InvoiceNumber = "INV-2026-0002",
                CardBrand = "visa",
                CardLast4 = "4242",
                StripeReceiptUrl = "https://pay.stripe.com/receipts/test_11",
            },
        ],
    };
}
