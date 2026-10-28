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
        Adults = 3,
        TotalAmount = 720m,
        PaidAmount = 720m,
        SuggestedRefundAmount = 360m,
        // 7 days before arrival, the last day of the 50% step
        RefundReferenceDate = new DateOnly(2026, 10, 10),
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
            Refundable(new() { RoomId = 1, RoomNumber = "101", RoomName = "Standard Room", RateName = "Standard", PricePerNight = 100m, Total = 300m, IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 50, RefundAmount = 150m }),
            Refundable(new() { RoomId = 3, RoomNumber = "103", RoomName = "Deluxe Room", RateName = "Flexible", PricePerNight = 140m, Total = 420m, IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 50, RefundAmount = 210m }),
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
    /// <summary>
    /// the same booking with 1 room on a non-refundable rate, nothing is refunded
    /// </summary>
    public static BookingDto.AdminDetail NonRefundable()
    {
        var booking = Full();
        booking.SuggestedRefundAmount = 0m;
        booking.Rooms =
        [
            new() { RoomId = 1, RoomNumber = "101", RoomName = "Standard Room", RateName = "Non-refundable", PricePerNight = 100m, Total = 300m, IsRefundable = false, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 0, RefundAmount = 0m },
        ];
        booking.TotalAmount = 300m;
        return booking;
    }

    // fills in the refund dates the server derives from the check-in of Full()
    private static BookingDto.AdminRoomDto Refundable(BookingDto.AdminRoomDto room)
    {
        var checkIn = new DateOnly(2026, 10, 17);
        room.FullRefundUntil = checkIn.AddDays(-room.FullRefundDays);
        room.PartialRefundUntil = checkIn.AddDays(-room.PartialRefundDays);
        return room;
    }
}
