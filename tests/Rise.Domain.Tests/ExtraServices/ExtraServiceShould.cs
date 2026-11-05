using Rise.Domain.ExtraServices;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.ExtraServices;

public class ExtraServiceShould
{
    [Fact]
    public void BeCreatedWithValidData()
    {
        var service = new ExtraService(
            "  Breakfast  ",
            15.50m,
            ChargeCadence.PerNight,
            isPerPerson: true,
            "Fresh buffet");

        service.Name.ShouldBe("Breakfast");
        service.Price.ShouldBe(15.50m);
        service.Cadence.ShouldBe(ChargeCadence.PerNight);
        service.IsPerPerson.ShouldBeTrue();
        service.Description.ShouldBe("Fresh buffet");
        service.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowWhenNameIsNullOrWhitespace(string? invalidName)
    {
        Action act = () => new ExtraService(invalidName!, 10m);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ThrowWhenPriceIsNegative()
    {
        Action act = () => new ExtraService("Parking", -5m);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AllowZeroPrice()
    {
        var service = new ExtraService("Free Wi-Fi", 0m);

        service.Price.ShouldBe(0m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void StoreEmptyDescriptionAsNull(string? description)
    {
        var service = new ExtraService("Cleaning", 30m, description: description);

        service.Description.ShouldBeNull();
    }

    [Fact]
    public void UpdateFieldsCorrectly()
    {
        var service = new ExtraService("Breakfast", 15m, ChargeCadence.PerNight, true);

        service.Update("Luxury Breakfast", 20m, ChargeCadence.Once, false, "Champagne breakfast");

        service.Name.ShouldBe("Luxury Breakfast");
        service.Price.ShouldBe(20m);
        service.Cadence.ShouldBe(ChargeCadence.Once);
        service.IsPerPerson.ShouldBeFalse();
        service.Description.ShouldBe("Champagne breakfast");
    }

    [Fact]
    public void ActivateAndDeactivate()
    {
        var service = new ExtraService("Sauna", 25m);
        service.IsActive.ShouldBeTrue();

        service.Deactivate();
        service.IsActive.ShouldBeFalse();

        service.Activate();
        service.IsActive.ShouldBeTrue();
    }
}
