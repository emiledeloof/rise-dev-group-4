using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Services.RoomTypes;
using Rise.Shared.RoomTypes;

namespace Rise.Services.IntegrationTests.RoomTypes;

[Collection(DatabaseCollection.Name)]
public class RoomTypeServiceShould(PostgresFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // saves the entities with its own context so the act step cannot reuse tracked objects
    private async Task ArrangeAsync(params object[] entities)
    {
        await using var dbContext = database.CreateDbContext();
        dbContext.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateARoomType()
    {
        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).CreateAsync(new RoomTypeRequest.Create
            {
                Name = "Deluxe Room",
                Description = "Big bed",
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value.Name.ShouldBe("Deluxe Room");
        }

        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.RoomTypes.SingleAsync();
        saved.Description.ShouldBe("Big bed");
    }

    [Fact]
    public async Task RefuseADuplicateNameWhateverTheCase()
    {
        await ArrangeAsync(new RoomType("Deluxe Room"));

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).CreateAsync(new RoomTypeRequest.Create { Name = "deluxe room" });

            result.Status.ShouldBe(ResultStatus.Conflict);
        }

        await using var assertContext = database.CreateDbContext();
        (await assertContext.RoomTypes.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task ListRoomTypesByNameWithTheirRoomNumbers()
    {
        var standard = new RoomType("Standard Room");
        var deluxe = new RoomType("Deluxe Room");
        // two physical rooms of the same type, added in reverse order to check the sort
        var room102 = new Room("102", "Standard Room", 2, standard);
        var room101 = new Room("101", "Standard Room", 2, standard);
        await ArrangeAsync(standard, deluxe, room102, room101);

        await using var dbContext = database.CreateDbContext();
        var result = await new RoomTypeService(dbContext).GetIndexAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoomTypes.Select(t => t.Name).ShouldBe(new[] { "Deluxe Room", "Standard Room" });
        result.Value.RoomTypes.Single(t => t.Name == "Standard Room").RoomNumbers.ShouldBe(new[] { "101", "102" });
        result.Value.RoomTypes.Single(t => t.Name == "Deluxe Room").RoomNumbers.ShouldBeEmpty();
    }

    [Fact]
    public async Task EditARoomType()
    {
        var roomType = new RoomType("Deluxe Room");
        await ArrangeAsync(roomType);

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
            {
                RoomTypeId = roomType.Id,
                Name = "Deluxe Suite",
                Description = "Sea view",
            });

            result.IsSuccess.ShouldBeTrue();
        }

        // read back from the database, not from the tracked object, to prove the change was saved
        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.RoomTypes.SingleAsync(t => t.Id == roomType.Id);
        saved.Name.ShouldBe("Deluxe Suite");
        saved.Description.ShouldBe("Sea view");
    }

    [Fact]
    public async Task KeepItsOwnNameWhenEditing()
    {
        var roomType = new RoomType("Deluxe Room");
        await ArrangeAsync(roomType);

        await using var dbContext = database.CreateDbContext();
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
        await using var dbContext = database.CreateDbContext();
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
        var roomType = new RoomType("Deluxe Room");
        await ArrangeAsync(roomType);

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).DeleteAsync(roomType.Id);

            result.IsSuccess.ShouldBeTrue();
        }

        await using var assertContext = database.CreateDbContext();
        (await assertContext.RoomTypes.AnyAsync(t => !t.IsDeleted)).ShouldBeFalse();
    }

    [Fact]
    public async Task RefuseToDeleteARoomTypeUsedByARoom()
    {
        var roomType = new RoomType("Deluxe Room");
        var room = new Room("102", "Deluxe Room", 2, roomType);
        await ArrangeAsync(room);

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).DeleteAsync(roomType.Id);

            result.Status.ShouldBe(ResultStatus.Conflict);
        }

        await using var assertContext = database.CreateDbContext();
        (await assertContext.RoomTypes.AnyAsync(t => !t.IsDeleted)).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveTheTickedAmenitiesAndTheBedConfiguration()
    {
        var roomType = new RoomType("Deluxe Room");
        var wifi = new Amenity("Wi-Fi", "fa-wifi");
        var bathtub = new Amenity("Bathtub", "fa-bath");
        roomType.AddAmenity(bathtub);
        await ArrangeAsync(roomType, wifi);

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
            {
                RoomTypeId = roomType.Id,
                Name = "Deluxe Room",
                AmenityIds = [wifi.Id],
                BedConfigurations = new() { ["en"] = "King bed", ["nl"] = "Kingsize bed", ["fr"] = "" },
            });

            result.IsSuccess.ShouldBeTrue();
        }

        await using var assertContext = database.CreateDbContext();
        var edit = (await new RoomTypeService(assertContext).GetForEditAsync(roomType.Id)).Value;
        edit.AmenityIds.ShouldBe([wifi.Id]);
        edit.BedConfigurations.Count.ShouldBe(2);
        edit.BedConfigurations["nl"].ShouldBe("Kingsize bed");
    }

    [Fact]
    public async Task CreateARoomTypeWithItsCharacteristics()
    {
        var wifi = new Amenity("Wi-Fi", "fa-wifi");
        await ArrangeAsync(wifi);

        int id;
        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new RoomTypeService(dbContext).CreateAsync(new RoomTypeRequest.Create
            {
                Name = "Deluxe Room",
                AmenityIds = [wifi.Id],
                BedConfigurations = new() { ["en"] = "King bed" },
            });
            id = result.Value.Id;
        }

        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.RoomTypes.Include(t => t.Amenities).SingleAsync(t => t.Id == id);
        saved.Amenities.ShouldHaveSingleItem().DefaultName.ShouldBe("Wi-Fi");
        saved.BedConfigurationIn("en").ShouldBe("King bed");
    }

    [Fact]
    public async Task KeepAnInactiveAmenityTheFormDoesNotShow()
    {
        var roomType = new RoomType("Deluxe Room");
        var bathtub = new Amenity("Bathtub", "fa-bath");
        bathtub.Deactivate();
        roomType.AddAmenity(bathtub);
        await ArrangeAsync(roomType);

        await using (var dbContext = database.CreateDbContext())
        {
            // the form only lists active amenities, so the bathtub is never sent
            var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
            {
                RoomTypeId = roomType.Id,
                Name = "Deluxe Room",
                AmenityIds = [],
            });

            result.IsSuccess.ShouldBeTrue();
        }

        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.RoomTypes.Include(t => t.Amenities).SingleAsync();
        saved.Amenities.ShouldHaveSingleItem().DefaultName.ShouldBe("Bathtub");
    }

    [Fact]
    public async Task RefuseAnUnknownAmenity()
    {
        var roomType = new RoomType("Deluxe Room");
        await ArrangeAsync(roomType);

        await using var dbContext = database.CreateDbContext();
        var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
        {
            RoomTypeId = roomType.Id,
            Name = "Deluxe Room",
            AmenityIds = [4242],
        });

        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task RefuseToAddAnInactiveAmenity()
    {
        var roomType = new RoomType("Deluxe Room");
        var bathtub = new Amenity("Bathtub", "fa-bath");
        bathtub.Deactivate();
        await ArrangeAsync(roomType, bathtub);

        await using var dbContext = database.CreateDbContext();
        var result = await new RoomTypeService(dbContext).EditAsync(new RoomTypeRequest.Edit
        {
            RoomTypeId = roomType.Id,
            Name = "Deluxe Room",
            AmenityIds = [bathtub.Id],
        });

        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ReturnNotFoundForTheEditFormOfAnUnknownRoomType()
    {
        await using var dbContext = database.CreateDbContext();

        var result = await new RoomTypeService(dbContext).GetForEditAsync(42);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }
}
