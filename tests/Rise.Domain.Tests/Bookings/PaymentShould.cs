using Rise.Domain.Bookings;
using Rise.Domain.Guests;

namespace Rise.Domain.Tests.Bookings;

public class PaymentShould
{
    private static Booking CreateBooking()
        => new("20261006-0042",
            new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium")),
            new DateOnly(2026, 10, 10),
            new DateOnly(2026, 10, 12));

    [Fact]
    public void BeCreatedAsUnpaid()
    {
        var booking = CreateBooking();

        var payment = booking.AddPayment(158m, "cs_test_123");

        booking.Payments.ShouldHaveSingleItem().ShouldBe(payment);
        payment.Amount.ShouldBe(158m);
        payment.StripeCheckoutSession.ShouldBe("cs_test_123");
        payment.IsPaid.ShouldBeFalse();
        payment.PaidAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWithAnInvalidAmount(decimal amount)
    {
        var booking = CreateBooking();

        Action act = () => booking.AddPayment(amount);

        act.ShouldThrow<ArgumentException>();
        booking.Payments.ShouldBeEmpty();
    }

    [Fact]
    public void BeMarkedAsPaid()
    {
        var payment = CreateBooking().AddPayment(158m);
        var paidAt = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

        payment.MarkAsPaid(paidAt, "in_test_123", "INV-2026-0001");

        payment.IsPaid.ShouldBeTrue();
        payment.PaidAt.ShouldBe(paidAt);
        payment.StripeInvoice.ShouldBe("in_test_123");
        payment.InvoiceNumber.ShouldBe("INV-2026-0001");
    }

    [Fact]
    public void NotBeMarkedAsPaidTwice()
    {
        var payment = CreateBooking().AddPayment(158m);
        payment.MarkAsPaid(DateTime.UtcNow);

        Action act = () => payment.MarkAsPaid(DateTime.UtcNow);

        act.ShouldThrow<InvalidOperationException>();
    }
}
