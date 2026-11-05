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
    public void RecordTheOutstandingBalanceAsAPaymentReceivedOnSite()
    {
        var booking = CreateBooking();

        booking.Settle(SettledAt, 146m);

        booking.IsSettled.ShouldBeTrue();
        booking.SettledAt.ShouldBe(SettledAt);
        var payment = booking.Payments.ShouldHaveSingleItem();
        payment.Amount.ShouldBe(146m);
        payment.PaidAt.ShouldBe(SettledAt);
        booking.PaidAmount.ShouldBe(146m);
    }

    // a paid bill or a credit for the guest, nothing to collect
    [Theory]
    [InlineData(0)]
    [InlineData(-20)]
    public void NotRecordAPaymentWhenNothingIsOwed(decimal outstandingBalance)
    {
        var booking = CreateBooking();

        booking.Settle(SettledAt, outstandingBalance);

        booking.IsSettled.ShouldBeTrue();
        booking.Payments.ShouldBeEmpty();
    }

    [Fact]
    public void NotBeSettledTwice()
    {
        var booking = CreateBooking();
        booking.Settle(SettledAt, 146m);

        Action act = () => booking.Settle(SettledAt.AddHours(1), 10m);

        act.ShouldThrow<InvalidOperationException>();
        booking.Payments.Count.ShouldBe(1);
    }

    [Fact]
    public void NotSettleACancelledBooking()
    {
        var booking = CreateBooking();
        booking.Cancel(SettledAt, "Guest is ill", 0m);

        Action act = () => booking.Settle(SettledAt, 146m);

        act.ShouldThrow<InvalidOperationException>();
        booking.IsSettled.ShouldBeFalse();
    }

    [Fact]
    public void RefuseNewBillLinesOnceSettled()
    {
        var booking = CreateBooking();
        booking.Settle(SettledAt, 0m);

        Action act = () => booking.AddBillLine("Minibar", 1, 8m, 22m);

        act.ShouldThrow<InvalidOperationException>();
    }
}
