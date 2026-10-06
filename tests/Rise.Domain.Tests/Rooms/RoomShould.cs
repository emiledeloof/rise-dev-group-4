using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Rooms;

public class RoomShould
{
    [Fact]
    public void BeCreated()
    {
        var room = new Room("101", "Single Room", 1, "Cosy room");

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
        Action act = () => new Room(roomNumber!, "Single Room", 1);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidName(string? name)
    {
        Action act = () => new Room("101", name!, 1);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWithInvalidMaxGuests(int maxGuests)
    {
        Action act = () => new Room("101", "Single Room", maxGuests);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddARate()
    {
        var room = new Room("101", "Single Room", 1);

        var rate = room.AddRate("Standard", 79m);

        room.Rates.ShouldHaveSingleItem().ShouldBe(rate);
        rate.Room.ShouldBe(room);
        rate.PricePerNight.ShouldBe(79m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NotAddARateWithAnInvalidPrice(decimal pricePerNight)
    {
        var room = new Room("101", "Single Room", 1);

        Action act = () => room.AddRate("Standard", pricePerNight);

        act.ShouldThrow<ArgumentException>();
        room.Rates.ShouldBeEmpty();
    }

    [Fact]
    public void AddAPhoto()
    {
        var room = new Room("101", "Single Room", 1);

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
        var room = new Room("101", "Single Room", 1);

        Action act = () => room.AddPhoto(url!);

        act.ShouldThrow<ArgumentException>();
    }
}
