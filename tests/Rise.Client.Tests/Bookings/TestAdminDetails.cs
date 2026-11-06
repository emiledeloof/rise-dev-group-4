using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// admin booking details for the page tests
/// </summary>
public static class TestAdminDetails
{
    // 7 days before arrival, the last day of the 50% step
    private static readonly DateOnly ReferenceDate = new(2026, 10, 10);

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
        ActiveAmount = 720m,
        RefundedAmount = 0m,
        RefundableAmount = 720m,
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
            Refundable(new() { BookingRoomId = 21, RoomId = 1, RoomNumber = "101", RoomName = "Standard Room", MaxGuests = 2, RefundReferenceDate = ReferenceDate, RateName = "Standard", PricePerNight = 100m, Total = 300m, IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 50, RefundAmount = 150m }),
            Refundable(new() { BookingRoomId = 23, RoomId = 3, RoomNumber = "103", RoomName = "Deluxe Room", MaxGuests = 2, RefundReferenceDate = ReferenceDate, RateName = "Flexible", PricePerNight = 140m, Total = 420m, IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 50, RefundAmount = 210m }),
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
    /// the same booking with all rooms cancelled on 10 oct, the refund goes on the 1st room
    /// </summary>
    public static BookingDto.AdminDetail Cancelled(decimal refund)
    {
        var booking = Full();
        booking.Status = "Cancelled";
        booking.ActiveAmount = 0m;
        booking.RefundedAmount = refund;
        booking.RefundableAmount = booking.PaidAmount - refund;
        booking.Rooms[0].Cancellation = Cancellation(new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), refund);
        booking.Rooms[1].Cancellation = Cancellation(new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), 0m);
        return booking;
    }

    /// <summary>
    /// the same booking for 2 guests where room 103 was cancelled on 5 oct with a refund of 210, room 101 stays
    /// </summary>
    public static BookingDto.AdminDetail PartlyCancelled()
    {
        var booking = Full();
        booking.Adults = 2;
        booking.ActiveAmount = 300m;
        booking.RefundedAmount = 210m;
        booking.RefundableAmount = 510m;
        booking.Rooms[1].Cancellation = Cancellation(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc), 210m);
        booking.Rooms[1].RefundReferenceDate = new DateOnly(2026, 10, 5);
        return booking;
    }

    /// <summary>
    /// the same booking where room 101 is refundable and room 103 is non-refundable
    /// </summary>
    public static BookingDto.AdminDetail MixedPolicies()
    {
        var booking = Full();
        var room = booking.Rooms[1];
        room.IsRefundable = false;
        room.FullRefundUntil = null;
        room.PartialRefundUntil = null;
        room.RefundPercent = 0;
        room.RefundAmount = 0m;
        return booking;
    }

    private static BookingDto.AdminRoomCancellationDto Cancellation(DateTime cancelledAt, decimal refund) => new()
    {
        CancelledAt = cancelledAt,
        Reason = "Guest is ill",
        RefundAmount = refund,
    };

    /// <summary>
    /// the same booking with 1 room on a non-refundable rate, nothing is refunded
    /// </summary>
    public static BookingDto.AdminDetail NonRefundable()
    {
        var booking = Full();
        booking.Rooms =
        [
            new() { BookingRoomId = 21, RoomId = 1, RoomNumber = "101", RoomName = "Standard Room", MaxGuests = 2, RefundReferenceDate = ReferenceDate, RateName = "Non-refundable", PricePerNight = 100m, Total = 300m, IsRefundable = false, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50, RefundPercent = 0, RefundAmount = 0m },
        ];
        booking.TotalAmount = 300m;
        booking.ActiveAmount = 300m;
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
