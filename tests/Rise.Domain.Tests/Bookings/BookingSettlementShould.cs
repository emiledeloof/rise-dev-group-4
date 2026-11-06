using Rise.Domain.Bookings;
using Rise.Domain.Guests;

namespace Rise.Domain.Tests.Bookings;

public class BookingSettlementShould
{
    private static readonly DateTime SettledAt = new(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);

    private static Booking CreateBooking()
        => new("20261006-0042",
            new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium")),
            new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);

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
        booking.Cancel(SettledAt, "Guest is ill", 0m);

        Action act = () => booking.Settle(SettledAt);

        act.ShouldThrow<InvalidOperationException>();
        booking.IsSettled.ShouldBeFalse();
    }

    [Fact]
    public void NotBeCancelledOnceSettled()
    {
        var booking = CreateBooking();
        booking.Settle(SettledAt);

        Action act = () => booking.Cancel(SettledAt.AddDays(1), "Guest is ill", 0m);

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
