using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Services.Rooms;
using Rise.Shared.Rooms;

namespace Rise.Services.IntegrationTests.Rooms;

/// <summary>
/// physical rooms on a real postgres, here the unique index on the room number really exists
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RoomAdminServiceShould(PostgresFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task ArrangeAsync(params object[] entities)
    {
        await using var dbContext = database.CreateDbContext();
        dbContext.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }

    private async Task<Result<int>> CreateAsync(string roomNumber, int roomTypeId)
    {
        await using var dbContext = database.CreateDbContext();
        return await new RoomAdminService(dbContext).CreateAsync(new RoomRequest.Create
        {
            RoomNumber = roomNumber,
            RoomTypeId = roomTypeId,
            MaxGuests = 2,
        });
    }

    [Fact]
    public async Task CreateARoomAndListItWithItsType()
    {
        var deluxe = new RoomType("Deluxe Room");
        await ArrangeAsync(deluxe);

        var created = await CreateAsync(" 104 ", deluxe.Id);

        created.IsSuccess.ShouldBeTrue();
        await using var dbContext = database.CreateDbContext();
        var list = await new RoomAdminService(dbContext).GetIndexAsync();
        var room = list.Value.ShouldHaveSingleItem();
        room.Id.ShouldBe(created.Value);
        room.RoomNumber.ShouldBe("104");
        room.RoomTypeName.ShouldBe("Deluxe Room");
    }

    [Fact]
    public async Task RefuseARoomNumberThatAlreadyExists()
    {
        var deluxe = new RoomType("Deluxe Room");
        await ArrangeAsync(deluxe, new Room("104", "Deluxe Room 104", 2, deluxe));

        var result = await CreateAsync("104", deluxe.Id);

        result.Status.ShouldBe(ResultStatus.Conflict);
        await using var dbContext = database.CreateDbContext();
        (await dbContext.Rooms.CountAsync()).ShouldBe(1);
    }

    /// <summary>
    /// the service says a deleted room frees its number, the database must agree
    /// </summary>
    [Fact]
    public async Task AllowTheNumberOfADeletedRoom()
    {
        var deluxe = new RoomType("Deluxe Room");
        await ArrangeAsync(deluxe, new Room("104", "Deluxe Room 104", 2, deluxe) { IsDeleted = true });

        var result = await CreateAsync("104", deluxe.Id);

        result.IsSuccess.ShouldBeTrue();
    }
}
