using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Rooms;
using Rise.Shared.Rooms;

namespace Rise.Services.Tests.Rooms;

public class RoomAdminServiceShould
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
    public async Task ListRoomsByNumberWithTheirType()
    {
        await using var dbContext = NewDbContext(nameof(ListRoomsByNumberWithTheirType));
        var deluxe = new RoomType("Deluxe Room");
        dbContext.Rooms.AddRange(
            new Room("102", "Deluxe Room", 2, deluxe),
            new Room("101", "Deluxe Room", 3, deluxe));
        await dbContext.SaveChangesAsync();

        var result = await new RoomAdminService(dbContext).GetIndexAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.RoomNumber).ShouldBe(new[] { "101", "102" });
        result.Value[0].RoomTypeName.ShouldBe("Deluxe Room");
        result.Value[0].MaxGuests.ShouldBe(3);
    }

    [Fact]
    public async Task NotListDeletedRooms()
    {
        await using var dbContext = NewDbContext(nameof(NotListDeletedRooms));
        var deluxe = new RoomType("Deluxe Room");
        dbContext.Rooms.AddRange(
            new Room("101", "Deluxe Room", 2, deluxe),
            new Room("102", "Deluxe Room", 2, deluxe) { IsDeleted = true });
        await dbContext.SaveChangesAsync();

        var result = await new RoomAdminService(dbContext).GetIndexAsync();

        result.Value.Select(r => r.RoomNumber).ShouldBe(new[] { "101" });
    }

    [Fact]
    public async Task CreateARoomWithItsType()
    {
        await using var dbContext = NewDbContext(nameof(CreateARoomWithItsType));
        var deluxe = new RoomType("Deluxe Room");
        dbContext.RoomTypes.Add(deluxe);
        await dbContext.SaveChangesAsync();

        var result = await new RoomAdminService(dbContext).CreateAsync(new RoomRequest.Create
        {
            RoomNumber = " 104 ",
            RoomTypeId = deluxe.Id,
            MaxGuests = 2,
        });

        result.IsSuccess.ShouldBeTrue();
        var saved = await dbContext.Rooms.Include(r => r.RoomType).SingleAsync();
        saved.Id.ShouldBe(result.Value);
        saved.RoomNumber.ShouldBe("104");
        saved.Name.ShouldBe("Deluxe Room 104");
        saved.RoomType.Name.ShouldBe("Deluxe Room");
        saved.MaxGuests.ShouldBe(2);
    }

    [Fact]
    public async Task RefuseARoomNumberThatAlreadyExists()
    {
        await using var dbContext = NewDbContext(nameof(RefuseARoomNumberThatAlreadyExists));
        var deluxe = new RoomType("Deluxe Room");
        dbContext.Rooms.Add(new Room("104", "Deluxe Room", 2, deluxe));
        await dbContext.SaveChangesAsync();

        var result = await new RoomAdminService(dbContext).CreateAsync(new RoomRequest.Create
        {
            RoomNumber = "104",
            RoomTypeId = deluxe.Id,
            MaxGuests = 2,
        });

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.Rooms.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task AllowTheNumberOfADeletedRoom()
    {
        await using var dbContext = NewDbContext(nameof(AllowTheNumberOfADeletedRoom));
        var deluxe = new RoomType("Deluxe Room");
        dbContext.Rooms.Add(new Room("104", "Deluxe Room", 2, deluxe) { IsDeleted = true });
        await dbContext.SaveChangesAsync();

        var result = await new RoomAdminService(dbContext).CreateAsync(new RoomRequest.Create
        {
            RoomNumber = "104",
            RoomTypeId = deluxe.Id,
            MaxGuests = 2,
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownRoomType()
    {
        await using var dbContext = NewDbContext(nameof(ReturnNotFoundForAnUnknownRoomType));

        var result = await new RoomAdminService(dbContext).CreateAsync(new RoomRequest.Create
        {
            RoomNumber = "104",
            RoomTypeId = 42,
            MaxGuests = 2,
        });

        result.Status.ShouldBe(ResultStatus.NotFound);
        (await dbContext.Rooms.CountAsync()).ShouldBe(0);
    }
}
