using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.RoomTypes;
using Rise.Shared.RoomTypes;

namespace Rise.Services.Tests.RoomTypes;

public class RoomTypeServiceShould
{
    // each test gives its own database name so the tests do not share data
    private static ApplicationDbContext NewDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateARoomType()
    {
        await using var dbContext = NewDbContext(nameof(CreateARoomType));

        var result = await new RoomTypeService(dbContext).CreateAsync(new RoomTypeRequest.Create
        {
            Name = "Deluxe Room",
            Description = "Big bed",
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Deluxe Room");
        var saved = await dbContext.RoomTypes.SingleAsync();
        saved.Description.ShouldBe("Big bed");
    }

    [Fact]
    public async Task RefuseADuplicateNameWhateverTheCase()
    {
        await using var dbContext = NewDbContext(nameof(RefuseADuplicateNameWhateverTheCase));
        dbContext.RoomTypes.Add(new RoomType("Deluxe Room"));
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).CreateAsync(new RoomTypeRequest.Create { Name = "deluxe room" });

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.RoomTypes.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task ListRoomTypesByNameWithTheirRoomNumbers()
    {
        await using var dbContext = NewDbContext(nameof(ListRoomTypesByNameWithTheirRoomNumbers));
        var standard = new RoomType("Standard Room");
        var deluxe = new RoomType("Deluxe Room");
        // two physical rooms of the same type, added in reverse order to check the sort
        var room102 = new Room("102", "Standard Room", 2);
        var room101 = new Room("101", "Standard Room", 2);
        room102.ChangeType(standard);
        room101.ChangeType(standard);
        dbContext.RoomTypes.AddRange(standard, deluxe);
        dbContext.Rooms.AddRange(room102, room101);
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).GetIndexAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoomTypes.Select(t => t.Name).ShouldBe(new[] { "Deluxe Room", "Standard Room" });
        result.Value.RoomTypes.Single(t => t.Name == "Standard Room").RoomNumbers.ShouldBe(new[] { "101", "102" });
        result.Value.RoomTypes.Single(t => t.Name == "Deluxe Room").RoomNumbers.ShouldBeEmpty();
    }

    [Fact]
    public async Task EditARoomType()
    {
        await using var dbContext = NewDbContext(nameof(EditARoomType));
        var roomType = new RoomType("Deluxe Room");
        dbContext.RoomTypes.Add(roomType);
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
        {
            RoomTypeId = roomType.Id,
            Name = "Deluxe Suite",
            Description = "Sea view",
        });

        result.IsSuccess.ShouldBeTrue();
        roomType.Name.ShouldBe("Deluxe Suite");
        roomType.Description.ShouldBe("Sea view");
    }

    [Fact]
    public async Task KeepItsOwnNameWhenEditing()
    {
        await using var dbContext = NewDbContext(nameof(KeepItsOwnNameWhenEditing));
        var roomType = new RoomType("Deluxe Room");
        dbContext.RoomTypes.Add(roomType);
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
        {
            RoomTypeId = roomType.Id,
            Name = "Deluxe Room",
            Description = "Only the description changes",
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnNotFoundWhenEditingAnUnknownRoomType()
    {
        await using var dbContext = NewDbContext(nameof(ReturnNotFoundWhenEditingAnUnknownRoomType));

        var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
        {
            RoomTypeId = 42,
            Name = "Deluxe Room",
        });

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteAnUnusedRoomType()
    {
        await using var dbContext = NewDbContext(nameof(DeleteAnUnusedRoomType));
        var roomType = new RoomType("Deluxe Room");
        dbContext.RoomTypes.Add(roomType);
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).DeleteAsync(roomType.Id);

        result.IsSuccess.ShouldBeTrue();
        (await dbContext.RoomTypes.AnyAsync(t => !t.IsDeleted)).ShouldBeFalse();
    }

    [Fact]
    public async Task RefuseToDeleteARoomTypeUsedByARoom()
    {
        await using var dbContext = NewDbContext(nameof(RefuseToDeleteARoomTypeUsedByARoom));
        var roomType = new RoomType("Deluxe Room");
        var room = new Room("102", "Deluxe Room", 2);
        room.ChangeType(roomType);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var result = await new RoomTypeService(dbContext).DeleteAsync(roomType.Id);

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.RoomTypes.AnyAsync(t => !t.IsDeleted)).ShouldBeTrue();
    }
}
