using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Services.Pricing;

namespace Rise.Services.Tests.Pricing;

/// <summary>
/// tests for <see cref="RefundCalculator"/>, plain c# so no db needed
/// the rate uses the client rules, 100% up to 30 days, 50% up to 7 days
/// </summary>
public class RefundCalculatorShould
{
    private static readonly DateOnly CheckIn = new(2026, 10, 31);

    // a booked room with a copy of the policy of its rate
    private static BookingRoom CreateBookingRoom(CancellationPolicy policy = CancellationPolicy.Refundable)
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Flexible", 79m);
        rate.CancellationPolicy = policy;

        var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261006-0042", guest, CheckIn, CheckIn.AddDays(2), 2);

        return booking.AddRoom(room, rate);
    }

    /// <summary>
    /// the pct depends on the nb of days before arrival
    /// the limits 30 and 7 still give the bigger refund, the guest can cancel until midnight of that day
    /// </summary>
    [Theory]
    [InlineData(60, 100)]
    [InlineData(30, 100)]
    [InlineData(29, 50)]
    [InlineData(7, 50)]
    [InlineData(6, 0)]
    [InlineData(0, 0)]
    public void GiveThePctOfTheRuleThatMatches(int daysBeforeArrival, int expected)
    {
        var cancelledOn = CheckIn.AddDays(-daysBeforeArrival);

        RefundCalculator.RefundPercent(CreateBookingRoom(), CheckIn, cancelledOn).ShouldBe(expected);
    }

    /// <summary>
    /// works across months, 25 sep to 31 oct is 36 days
    /// </summary>
    [Fact]
    public void CountTheDaysAcrossMonths()
    {
        var cancelledOn = new DateOnly(2026, 9, 25);

        RefundCalculator.RefundPercent(CreateBookingRoom(), CheckIn, cancelledOn).ShouldBe(100);
    }

    /// <summary>
    /// ! a non refundable rate gives nothing back, even a long time before arrival
    /// </summary>
    [Fact]
    public void GiveNothingForANonRefundableRate()
    {
        var cancelledOn = CheckIn.AddDays(-100);

        RefundCalculator.RefundPercent(CreateBookingRoom(CancellationPolicy.NonRefundable), CheckIn, cancelledOn).ShouldBe(0);
    }
}
