using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rates;

public class RateShould
{
    [Fact]
    public void BelongToItsRoomAndKeepItsPricingDetails()
    {
        var room = new Room("101", "Single Room", 1);

        var rate = room.AddRate(
            "  Standard  ",
            79m,
            "Flexible cancellation",
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31));

        rate.Room.ShouldBe(room);
        rate.Name.ShouldBe("Standard");
        rate.PricePerNight.ShouldBe(79m);
        rate.IsActive.ShouldBeTrue();
        rate.Description.ShouldBe("Flexible cancellation");
        rate.StartDate.ShouldBe(new DateOnly(2026, 10, 1));
        rate.EndDate.ShouldBe(new DateOnly(2026, 10, 31));
        room.Rates.ShouldContain(rate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectAnEmptyName(string? name)
    {
        var room = new Room("101", "Single Room", 1);

        Action act = () => room.AddRate(name!, 79m);

        act.ShouldThrow<ArgumentException>();
        room.Rates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectANonPositivePrice(decimal price)
    {
        var room = new Room("101", "Single Room", 1);

        Action act = () => room.AddRate("Standard", price);

        act.ShouldThrow<ArgumentException>();
        room.Rates.ShouldBeEmpty();
    }

    /// <summary>
    /// a new rate uses the client rules, 100% up to 30 days, 50% up to 7 days
    /// </summary>
    [Fact]
    public void BeRefundableByDefault()
    {
        var rate = new Room("101", "Single Room", 1).AddRate("Standard", 79m);

        rate.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        rate.FullRefundDays.ShouldBe(30);
        rate.PartialRefundDays.ShouldBe(7);
        rate.PartialRefundPercent.ShouldBe(50);
    }

    /// <summary>
    /// a negative nb of days is refused by the Guard
    /// </summary>
    [Fact]
    public void RejectNegativeRefundDays()
    {
        var rate = new Room("101", "Single Room", 1).AddRate("Standard", 79m);

        Should.Throw<ArgumentException>(() => rate.FullRefundDays = -1);
        Should.Throw<ArgumentException>(() => rate.PartialRefundDays = -1);
    }

    /// <summary>
    /// a pct outside 0 to 100 is refused by the Guard
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RejectAPartialRefundOutsideZeroToHundred(int percent)
    {
        var rate = new Room("101", "Single Room", 1).AddRate("Standard", 79m);

        Should.Throw<ArgumentOutOfRangeException>(() => rate.PartialRefundPercent = percent);
    }

}
