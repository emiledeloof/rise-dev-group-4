using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Services.Bookings;
using Rise.Shared.Bookings;

namespace Rise.Services.Tests.Bookings;

/// <summary>
/// tests for <see cref="BookingStatusRule"/>, plain c# so no db needed
/// </summary>
public class BookingStatusRuleShould
{
    private static Booking CreateBooking()
    {
        var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));
        return new Booking("20261006-0042", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
    }

    [Theory]
    [InlineData(false, false, BookingStatus.Pending)]
    [InlineData(false, true, BookingStatus.Paid)]
    [InlineData(true, false, BookingStatus.Cancelled)]
    [InlineData(true, true, BookingStatus.Cancelled)]
    public void LetCancelledWinOverPaid(bool isCancelled, bool hasSettledPayment, string expected)
    {
        BookingStatusRule.For(isCancelled, hasSettledPayment).ShouldBe(expected);
    }

    [Fact]
    public void BePendingWithAnOpenPayment()
    {
        var booking = CreateBooking();
        booking.AddPayment(100m);

        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Pending);
    }

    [Fact]
    public void BePaidWithASettledPayment()
    {
        var booking = CreateBooking();
        booking.AddPayment(100m).MarkAsPaid(DateTime.UtcNow);

        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Paid);
    }

    [Fact]
    public void IgnoreADeletedSettledPayment()
    {
        var booking = CreateBooking();
        var payment = booking.AddPayment(100m);
        payment.MarkAsPaid(DateTime.UtcNow);
        payment.IsDeleted = true;

        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Pending);
    }

    [Fact]
    public void BeCancelledWhenTheBookingIsCancelled()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking, "101");
        booking.AddPayment(100m).MarkAsPaid(DateTime.UtcNow);
        booking.CancelRooms([new RoomCancellation(room, 0m)], DateTime.UtcNow, "Guest is ill");

        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public void StayPaidWhenOnlyARoomIsCancelled()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking, "101");
        AddRoom(booking, "102");
        booking.AddPayment(100m).MarkAsPaid(DateTime.UtcNow);
        booking.CancelRooms([new RoomCancellation(room, 0m)], DateTime.UtcNow, "Guest is ill");

        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Paid);
    }

    [Theory]
    [InlineData(0, 0, 0, BookingRefundStatus.None)]
    [InlineData(2, 0, 0, BookingRefundStatus.NoRefund)]
    [InlineData(2, 2, 1, BookingRefundStatus.RefundPending)]
    [InlineData(2, 2, 0, BookingRefundStatus.Refunded)]
    [InlineData(2, 1, 0, BookingRefundStatus.Refunded)]
    public void LetAnOpenRefundWinOverAPaidOne(int cancelledRooms, int roomsWithRefund, int openRefunds, string expected)
    {
        BookingStatusRule.RefundStatusFor(cancelledRooms, roomsWithRefund, openRefunds).ShouldBe(expected);
    }

    [Fact]
    public void HaveNoRefundStatusWithoutCancelledRooms()
    {
        var booking = CreateBooking();
        AddRoom(booking, "101");

        BookingStatusRule.RefundStatusFor(booking).ShouldBe(BookingRefundStatus.None);
    }

    [Fact]
    public void HaveAPendingRefundUntilItIsPaid()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking, "101");
        AddRoom(booking, "102");
        booking.AddPayment(200m).MarkAsPaid(DateTime.UtcNow);
        booking.CancelRooms([new RoomCancellation(room, 50m)], DateTime.UtcNow, "Guest is ill");

        BookingStatusRule.RefundStatusFor(booking).ShouldBe(BookingRefundStatus.RefundPending);

        booking.RecordRefundPayment(room, DateOnly.FromDateTime(DateTime.UtcNow), 50m);

        BookingStatusRule.RefundStatusFor(booking).ShouldBe(BookingRefundStatus.Refunded);
        BookingStatusRule.For(booking).ShouldBe(BookingStatus.Paid);
    }

    [Fact]
    public void HaveNoRefundWhenNothingIsRefunded()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking, "101");
        booking.AddPayment(100m).MarkAsPaid(DateTime.UtcNow);
        booking.CancelRooms([new RoomCancellation(room, 0m)], DateTime.UtcNow, "Guest is ill");

        BookingStatusRule.RefundStatusFor(booking).ShouldBe(BookingRefundStatus.NoRefund);
    }

    private static BookingRoom AddRoom(Booking booking, string roomNumber)
    {
        var room = new Room(roomNumber, "Twin Room", 2, new RoomType("Standard"));
        return booking.AddRoom(room, room.AddRate("Standard", 100m));
    }
}
