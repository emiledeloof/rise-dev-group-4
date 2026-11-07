using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rooms;

public class RoomTypeShould
{
    [Fact]
    public void BeCreatedWithATrimmedName()
    {
        var roomType = new RoomType("  Deluxe Room  ", "Big bed");

        roomType.Name.ShouldBe("Deluxe Room");
        roomType.Description.ShouldBe("Big bed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBeCreatedWithAnInvalidName(string? name)
    {
        Action act = () => new RoomType(name!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void StoreAnEmptyDescriptionAsNull()
    {
        var roomType = new RoomType("Deluxe Room", "   ");

        roomType.Description.ShouldBeNull();
    }

    [Fact]
    public void BeLinkedToARoom()
    {
        var roomType = new RoomType("Deluxe Room");
        var room = new Room("102", "Deluxe Room", 2, roomType);

        room.RoomType.ShouldBe(roomType);
    }

    [Fact]
    public void HaveNoAmenitiesAndNoBedConfigurationByDefault()
    {
        var roomType = new RoomType("Deluxe Room");

        roomType.Amenities.ShouldBeEmpty();
        roomType.BedConfigurationIn("en").ShouldBeNull();
    }

    [Fact]
    public void AddAnAmenity()
    {
        var roomType = new RoomType("Deluxe Room");
        var wifi = new Amenity("Wi-Fi", "fa-wifi");

        roomType.AddAmenity(wifi);

        roomType.Amenities.ShouldHaveSingleItem().ShouldBe(wifi);
    }

    [Fact]
    public void NotAddTheSameAmenityTwice()
    {
        var roomType = new RoomType("Deluxe Room");
        var wifi = new Amenity("Wi-Fi", "fa-wifi");

        roomType.AddAmenity(wifi);
        roomType.AddAmenity(wifi);

        roomType.Amenities.ShouldHaveSingleItem();
    }

    [Fact]
    public void OfferExactlyTheReplacedAmenities()
    {
        var wifi = new Amenity("Wi-Fi", "fa-wifi");
        var tv = new Amenity("Flat-screen TV", "fa-tv");
        var bathtub = new Amenity("Bathtub", "fa-bath");
        var roomType = new RoomType("Deluxe Room");
        roomType.AddAmenity(wifi);
        roomType.AddAmenity(tv);

        roomType.ReplaceAmenities([tv, bathtub, bathtub]);

        roomType.Amenities.ShouldBe([tv, bathtub]);
    }

    [Fact]
    public void ShowTheBedConfigurationInTheAskedLanguage()
    {
        var roomType = new RoomType("Standard Room");
        roomType.SetBedConfiguration("en", "  2 single beds  ");
        roomType.SetBedConfiguration("nl", "2 eenpersoonsbedden");

        roomType.BedConfigurationIn("en").ShouldBe("2 single beds");
        roomType.BedConfigurationIn("NL").ShouldBe("2 eenpersoonsbedden");
    }

    [Fact]
    public void FallBackToTheDefaultLanguageForAMissingBedConfiguration()
    {
        var roomType = new RoomType("Standard Room");
        roomType.SetBedConfiguration("en", "2 single beds");

        roomType.BedConfigurationIn("fr").ShouldBe("2 single beds");
    }

    [Fact]
    public void RemoveABedConfigurationSetToEmpty()
    {
        var roomType = new RoomType("Standard Room");
        roomType.SetBedConfiguration("de", "2 Einzelbetten");

        roomType.SetBedConfiguration("de", "  ");

        roomType.Translations.ShouldBeEmpty();
    }

    [Fact]
    public void RefuseABedConfigurationInAnUnsupportedLanguage()
    {
        var roomType = new RoomType("Standard Room");

        Action act = () => roomType.SetBedConfiguration("es", "2 camas");

        act.ShouldThrow<ArgumentException>();
    }
}
