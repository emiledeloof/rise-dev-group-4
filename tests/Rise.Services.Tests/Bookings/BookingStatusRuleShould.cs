using Rise.Services.Bookings;
using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Services.Tests.Bookings;

public class BookingStatusRuleShould
{
    [Theory]
    [InlineData(true, false, true, BookingStatus.Cancelled)]
    [InlineData(false, true, false, BookingStatus.Settled)]
    [InlineData(false, true, true, BookingStatus.Settled)]
    [InlineData(false, false, true, BookingStatus.Paid)]
    [InlineData(false, false, false, BookingStatus.Pending)]
    public void GiveTheStrongestStatus(bool isCancelled, bool isBillSettled, bool hasSettledPayment, string expected)
    {
        BookingStatusRule.For(isCancelled, isBillSettled, hasSettledPayment).ShouldBe(expected);
    }
}
