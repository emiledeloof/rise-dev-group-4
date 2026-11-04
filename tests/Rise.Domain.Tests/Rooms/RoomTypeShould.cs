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
        var room = new Room("102", "Deluxe Room", 2);

        room.ChangeType(roomType);

        room.RoomType.ShouldBe(roomType);
    }
}
