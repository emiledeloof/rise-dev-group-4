using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rooms;

public class AmenityShould
{
    [Fact]
    public void BeCreatedActiveWithItsDefaultNameAndIcon()
    {
        var amenity = new Amenity("  Wi-Fi  ", " fa-wifi ");

        amenity.DefaultName.ShouldBe("Wi-Fi");
        amenity.Icon.ShouldBe("fa-wifi");
        amenity.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBeCreatedWithoutADefaultName(string? name)
    {
        Action act = () => new Amenity(name!, "fa-wifi");

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBeCreatedWithoutAnIcon(string? icon)
    {
        Action act = () => new Amenity("Wi-Fi", icon!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShowTheNameInTheAskedLanguage()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");
        amenity.SetName("de", "Haartrockner");

        amenity.NameIn("de").ShouldBe("Haartrockner");
        amenity.NameIn("DE").ShouldBe("Haartrockner");
    }

    [Fact]
    public void FallBackToTheDefaultNameForAMissingTranslation()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");

        amenity.NameIn("it").ShouldBe("Hairdryer");
    }

    [Fact]
    public void ChangeAnExistingTranslation()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");

        amenity.SetName("en", "Hair dryer");

        amenity.Translations.ShouldHaveSingleItem().Name.ShouldBe("Hair dryer");
    }

    [Fact]
    public void RemoveATranslationSetToEmpty()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");
        amenity.SetName("fr", "Sèche-cheveux");

        amenity.SetName("fr", "");

        amenity.Translations.ShouldHaveSingleItem().Language.ShouldBe("en");
    }

    [Fact]
    public void NeverRemoveTheDefaultName()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");

        Action act = () => amenity.SetName("en", " ");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void RefuseAnUnsupportedLanguage()
    {
        var amenity = new Amenity("Hairdryer", "fa-wind");

        Action act = () => amenity.SetName("es", "Secador");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void BeDeactivatedAndActivatedAgain()
    {
        var amenity = new Amenity("Wi-Fi", "fa-wifi");

        amenity.Deactivate();
        amenity.IsActive.ShouldBeFalse();

        amenity.Activate();
        amenity.IsActive.ShouldBeTrue();
    }
}
