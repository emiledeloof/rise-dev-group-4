using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Bookings;

public class BookingShould
{
    private static readonly DateOnly CheckIn = new(2026, 10, 10);
    private static readonly DateOnly CheckOut = new(2026, 10, 12);

    private static Guest CreateGuest()
        => new("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));

    [Fact]
    public void BeCreated()
    {
        var guest = CreateGuest();

        var booking = new Booking("20261006-0042", guest, CheckIn, CheckOut);

        booking.BookingNumber.ShouldBe("20261006-0042");
        booking.Guest.ShouldBe(guest);
        booking.CheckIn.ShouldBe(CheckIn);
        booking.CheckOut.ShouldBe(CheckOut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidBookingNumber(string? bookingNumber)
    {
        Action act = () => new Booking(bookingNumber!, CreateGuest(), CheckIn, CheckOut);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWhenCheckOutIsNotAfterCheckIn(int nights)
    {
        Action act = () => new Booking("20261006-0042", CreateGuest(), CheckIn, CheckIn.AddDays(nights));

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CopyThePriceOfTheRateWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);

        var bookingRoom = booking.AddRoom(room, rate);

        booking.Rooms.ShouldHaveSingleItem().ShouldBe(bookingRoom);
        bookingRoom.PricePerNight.ShouldBe(79m);

        rate.PricePerNight = 99m;
        bookingRoom.PricePerNight.ShouldBe(79m);
    }

    [Fact]
    public void NotAddARoomWithARateOfAnotherRoom()
    {
        var room = new Room("101", "Single Room", 1);
        var otherRoom = new Room("102", "Double Room", 2);
        var otherRate = otherRoom.AddRate("Standard", 109m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);

        Action act = () => booking.AddRoom(room, otherRate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public void NotAddTheSameRoomTwice()
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);
        booking.AddRoom(room, rate);

        Action act = () => booking.AddRoom(room, rate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.Count.ShouldBe(1);
    }

    /// <summary>
    /// ! the booking keeps the cancellation policy valid at booking time, a later change on the rate doesnt touch it
    /// </summary>
    [Fact]
    public void CopyTheCancellationPolicyOfTheRateWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Flexible", 79m);
        rate.FullRefundDays = 45;
        rate.PartialRefundDays = 10;
        rate.PartialRefundPercent = 30;
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);

        var bookingRoom = booking.AddRoom(room, rate);

        bookingRoom.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        bookingRoom.FullRefundDays.ShouldBe(45);
        bookingRoom.PartialRefundDays.ShouldBe(10);
        bookingRoom.PartialRefundPercent.ShouldBe(30);

        // the admin changes the rate after the booking
        rate.CancellationPolicy = CancellationPolicy.NonRefundable;
        rate.FullRefundDays = 60;
        rate.PartialRefundPercent = 0;

        bookingRoom.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        bookingRoom.FullRefundDays.ShouldBe(45);
        bookingRoom.PartialRefundPercent.ShouldBe(30);
    }

    private static Booking CreatePaidBooking(decimal paid = 200m)
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);
        booking.AddPayment(paid).MarkAsPaid(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        return booking;
    }

    [Fact]
    public void BeCancelledWithReasonAndRefund()
    {
        var booking = CreatePaidBooking();
        var cancelledAt = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

        booking.Cancel(cancelledAt, "  Guest is ill  ", 100m);

        booking.IsCancelled.ShouldBeTrue();
        booking.CancelledAt.ShouldBe(cancelledAt);
        booking.CancellationReason.ShouldBe("Guest is ill");
        booking.RefundAmount.ShouldBe(100m);
    }

    [Fact]
    public void KeepItsTermsWhenCancelled()
    {
        var room = new Room("101", "Single Room", 1);
        var booking = CreatePaidBooking();
        booking.AddRoom(room, room.AddRate("Flexible", 79m));

        booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m);

        booking.CheckIn.ShouldBe(CheckIn);
        booking.CheckOut.ShouldBe(CheckOut);
        booking.Rooms.Single().PricePerNight.ShouldBe(79m);
        booking.Payments.Single().IsPaid.ShouldBeTrue();
    }

    [Fact]
    public void NotBeCancelledTwice()
    {
        var booking = CreatePaidBooking();
        booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m);

        Action act = () => booking.Cancel(DateTime.UtcNow, "Again", 0m);

        act.ShouldThrow<InvalidOperationException>();
        booking.CancellationReason.ShouldBe("Guest is ill");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBeCancelledWithoutReason(string? reason)
    {
        var booking = CreatePaidBooking();

        Action act = () => booking.Cancel(DateTime.UtcNow, reason!, 0m);

        act.ShouldThrow<ArgumentException>();
        booking.IsCancelled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(200.01)]
    public void NotBeCancelledWithARefundOutsideTheAmountPaid(decimal refund)
    {
        var booking = CreatePaidBooking(200m);

        Action act = () => booking.Cancel(DateTime.UtcNow, "Guest is ill", refund);

        act.ShouldThrow<ArgumentException>();
        booking.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void NotRefundAnUnpaidPayment()
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);
        booking.AddPayment(200m);

        Action act = () => booking.Cancel(DateTime.UtcNow, "Guest is ill", 1m);

        booking.PaidAmount.ShouldBe(0m);
        act.ShouldThrow<ArgumentException>();
    }
}
