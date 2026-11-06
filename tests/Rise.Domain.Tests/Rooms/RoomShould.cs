using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rooms;

public class RoomShould
{
    [Fact]
    public void BeCreated()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"), "Cosy room");

        room.RoomNumber.ShouldBe("101");
        room.Name.ShouldBe("Single Room");
        room.MaxGuests.ShouldBe(1);
        room.Description.ShouldBe("Cosy room");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidRoomNumber(string? roomNumber)
    {
        Action act = () => new Room(roomNumber!, "Single Room", 1, new RoomType("Standard"));

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void NotBeCreatedWithoutARoomType()
    {
        Action act = () => new Room("101", "Single Room", 1, null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidName(string? name)
    {
        Action act = () => new Room("101", name!, 1, new RoomType("Standard"));

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWithInvalidMaxGuests(int maxGuests)
    {
        Action act = () => new Room("101", "Single Room", maxGuests, new RoomType("Standard"));

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddARate()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        var rate = room.AddRate("Standard", 79m);

        room.Rates.ShouldHaveSingleItem().ShouldBe(rate);
        rate.Room.ShouldBe(room);
        rate.PricePerNight.ShouldBe(79m);
    }

    /// <summary>
    /// the conditions given to AddRate are kept on the rate
    /// </summary>
    [Fact]
    public void AddARateWithConditions()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        var rate = room.AddRate("Non-refundable", 79m, "No refund after booking");

        rate.Description.ShouldBe("No refund after booking");
    }

    /// <summary>
    /// conditions are optional, the existing calls without them still work
    /// </summary>
    [Fact]
    public void AddARateWithoutConditions()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        var rate = room.AddRate("Standard", 79m);

        rate.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NotAddARateWithAnInvalidPrice(decimal pricePerNight)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        Action act = () => room.AddRate("Standard", pricePerNight);

        act.ShouldThrow<ArgumentException>();
        room.Rates.ShouldBeEmpty();
    }

    [Fact]
    public void AddAPhoto()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        var photo = room.AddPhoto("https://example.com/room.jpg");

        room.Photos.ShouldHaveSingleItem().ShouldBe(photo);
        photo.Url.ShouldBe("https://example.com/room.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotAddAPhotoWithAnInvalidUrl(string? url)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        Action act = () => room.AddPhoto(url!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void HaveNoCharacteristicsByDefault()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        room.SizeInSquareMeters.ShouldBeNull();
        room.BedType.ShouldBeNull();
        room.View.ShouldBeNull();
        room.Amenities.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NotAcceptAnInvalidSize(int size)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        Action act = () => room.SizeInSquareMeters = size;

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// an empty text is stored as unknown, so the page shows no blank value
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StoreAnEmptyBedTypeOrViewAsUnknown(string value)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"))
        {
            BedType = value,
            View = value,
        };

        room.BedType.ShouldBeNull();
        room.View.ShouldBeNull();
    }

    [Fact]
    public void HaveNoPoliciesByDefault()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));

        room.Policies.ShouldBeNull();
    }

    [Fact]
    public void TrimThePolicies()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"))
        {
            Policies = "  No pets allowed.  ",
        };

        room.Policies.ShouldBe("No pets allowed.");
    }

    /// <summary>
    /// an empty text is stored as no policies, so the page shows its empty state
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void StoreEmptyPoliciesAsUnknown(string? value)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"))
        {
            Policies = value,
        };

        room.Policies.ShouldBeNull();
    }

    [Fact]
    public void AddAnAmenity()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var wifi = new Amenity("Wi-Fi", "fa-wifi");

        room.AddAmenity(wifi);

        room.Amenities.ShouldHaveSingleItem().ShouldBe(wifi);
    }

    [Fact]
    public void NotAddTheSameAmenityTwice()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var wifi = new Amenity("Wi-Fi");

        room.AddAmenity(wifi);
        room.AddAmenity(wifi);

        room.Amenities.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotCreateAnAmenityWithAnInvalidName(string? name)
    {
        Action act = () => new Amenity(name!);

        act.ShouldThrow<ArgumentException>();
    }
}
