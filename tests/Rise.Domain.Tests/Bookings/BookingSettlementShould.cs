using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Bookings;

public class BookingSettlementShould
{
    private static readonly DateTime SettledAt = new(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);

    private static Booking CreateBooking()
        => new("20261006-0042",
            new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium")),
            new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);

    // a booking is cancelled when all its rooms are, so the tests that cancel need one
    private static BookingRoom AddRoom(Booking booking)
    {
        var room = new Room("101", "Standard Room", 2, new RoomType("Standard"));
        return booking.AddRoom(room, room.AddRate("Standard", 100m));
    }

    [Fact]
    public void BeSettled()
    {
        var booking = CreateBooking();

        booking.Settle(SettledAt);

        booking.IsSettled.ShouldBeTrue();
        booking.SettledAt.ShouldBe(SettledAt);
    }

    // payments are recorded by the admin, settling never adds one
    [Fact]
    public void NotRecordAPaymentWhenSettled()
    {
        var booking = CreateBooking();

        booking.Settle(SettledAt);

        booking.Payments.ShouldBeEmpty();
    }

    [Fact]
    public void NotBeSettledTwice()
    {
        var booking = CreateBooking();
        booking.Settle(SettledAt);

        Action act = () => booking.Settle(SettledAt.AddHours(1));

        act.ShouldThrow<InvalidOperationException>();
        booking.SettledAt.ShouldBe(SettledAt);
    }

    [Fact]
    public void NotSettleACancelledBooking()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking);
        booking.CancelRooms([new RoomCancellation(room, 0m)], SettledAt, "Guest is ill");

        Action act = () => booking.Settle(SettledAt);

        act.ShouldThrow<InvalidOperationException>();
        booking.IsSettled.ShouldBeFalse();
    }

    [Fact]
    public void NotBeCancelledOnceSettled()
    {
        var booking = CreateBooking();
        var room = AddRoom(booking);
        booking.Settle(SettledAt);

        Action act = () => booking.CancelRooms([new RoomCancellation(room, 0m)], SettledAt.AddDays(1), "Guest is ill");

        act.ShouldThrow<InvalidOperationException>();
        booking.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void RefuseNewBillLinesOnceSettled()
    {
        var booking = CreateBooking();
        booking.Settle(SettledAt);

        Action act = () => booking.AddBillLine("Minibar", 1, 8m, 22m);

        act.ShouldThrow<InvalidOperationException>();
    }
}
