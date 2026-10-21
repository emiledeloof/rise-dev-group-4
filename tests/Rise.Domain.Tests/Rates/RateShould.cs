using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rates;

public class RateShould
{
    [Fact]
    public void BelongToItsRoomAndKeepItsPricingDetails()
    {
        var room = new Room("101", "Single Room", 1);

        var rate = room.AddRate("  Standard  ", 79m, "Flexible cancellation");

        rate.Room.ShouldBe(room);
        rate.Name.ShouldBe("Standard");
        rate.PricePerNight.ShouldBe(79m);
        rate.Description.ShouldBe("Flexible cancellation");
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
}
