using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Services.Amenities;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Shared.Amenities;

namespace Rise.Services.IntegrationTests.Amenities;

[Collection(DatabaseCollection.Name)]
public class AmenityServiceShould(PostgresFixture database) : IAsyncLifetime
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

    private static Amenity Wifi()
    {
        var wifi = new Amenity("Wi-Fi", "fa-wifi");
        wifi.SetName("nl", "Wifi");
        return wifi;
    }

    [Fact]
    public async Task CreateAnActiveAmenityWithItsNames()
    {
        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new AmenityService(dbContext).CreateAsync(new AmenityRequest.Create
            {
                Names = new() { ["en"] = "Hairdryer", ["de"] = "Haartrockner", ["fr"] = "  " },
                Icon = "fa-wind",
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value.Name.ShouldBe("Hairdryer");
        }

        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.Amenities.SingleAsync();
        saved.IsActive.ShouldBeTrue();
        saved.Icon.ShouldBe("fa-wind");
        saved.Translations.Select(t => t.Language).Order().ShouldBe(["de", "en"]);
    }

    [Fact]
    public async Task NotAssignANewAmenityToAnyRoomType()
    {
        await ArrangeAsync(new RoomType("Deluxe Room"));

        await using (var dbContext = database.CreateDbContext())
        {
            await new AmenityService(dbContext).CreateAsync(new AmenityRequest.Create
            {
                Names = new() { ["en"] = "Hairdryer" },
                Icon = "fa-wind",
            });
        }

        await using var assertContext = database.CreateDbContext();
        var roomType = await assertContext.RoomTypes.Include(t => t.Amenities).SingleAsync();
        roomType.Amenities.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("en", "wi-fi")]
    [InlineData("nl", "WIFI")]
    public async Task RefuseADuplicateNameWhateverTheCase(string language, string name)
    {
        await ArrangeAsync(Wifi());

        await using (var dbContext = database.CreateDbContext())
        {
            var names = new Dictionary<string, string?> { ["en"] = "Wireless internet", [language] = name };
            var result = await new AmenityService(dbContext).CreateAsync(new AmenityRequest.Create { Names = names, Icon = "fa-wifi" });

            result.Status.ShouldBe(ResultStatus.Conflict);
            result.Errors.ShouldHaveSingleItem().ShouldContain(name);
        }

        await using var assertContext = database.CreateDbContext();
        (await assertContext.Amenities.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task AllowTheSameNameInAnOtherLanguage()
    {
        await ArrangeAsync(Wifi());

        await using var dbContext = database.CreateDbContext();
        var result = await new AmenityService(dbContext).CreateAsync(new AmenityRequest.Create
        {
            Names = new() { ["en"] = "Wireless internet", ["de"] = "Wifi" },
            Icon = "fa-wifi",
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ShowARenameOnEveryRoomType()
    {
        var wifi = Wifi();
        var deluxe = new RoomType("Deluxe Room");
        var standard = new RoomType("Standard Room");
        deluxe.AddAmenity(wifi);
        standard.AddAmenity(wifi);
        await ArrangeAsync(deluxe, standard);

        await using (var dbContext = database.CreateDbContext())
        {
            var result = await new AmenityService(dbContext).EditAsync(new AmenityRequest.Edit
            {
                AmenityId = wifi.Id,
                Names = new() { ["en"] = "Free Wi-Fi", ["nl"] = "Gratis wifi" },
                Icon = "fa-tower-broadcast",
            });

            result.IsSuccess.ShouldBeTrue();
        }

        await using var assertContext = database.CreateDbContext();
        var roomTypes = await assertContext.RoomTypes.Include(t => t.Amenities).ToListAsync();
        roomTypes.ShouldAllBe(t => t.Amenities.Single().DefaultName == "Free Wi-Fi" && t.Amenities.Single().Icon == "fa-tower-broadcast");
        roomTypes[0].Amenities.Single().NameIn("nl").ShouldBe("Gratis wifi");
    }

    [Fact]
    public async Task KeepItsOwnNamesWhenEditing()
    {
        var wifi = Wifi();
        await ArrangeAsync(wifi);

        await using var dbContext = database.CreateDbContext();
        var result = await new AmenityService(dbContext).EditAsync(new AmenityRequest.Edit
        {
            AmenityId = wifi.Id,
            Names = new() { ["en"] = "Wi-Fi", ["nl"] = "Wifi" },
            Icon = "fa-wifi",
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveATranslationClearedInTheForm()
    {
        var wifi = Wifi();
        await ArrangeAsync(wifi);

        await using (var dbContext = database.CreateDbContext())
        {
            await new AmenityService(dbContext).EditAsync(new AmenityRequest.Edit
            {
                AmenityId = wifi.Id,
                Names = new() { ["en"] = "Wi-Fi", ["nl"] = "" },
                Icon = "fa-wifi",
            });
        }

        await using var assertContext = database.CreateDbContext();
        var saved = await assertContext.Amenities.SingleAsync();
        saved.Translations.ShouldHaveSingleItem().Language.ShouldBe("en");
    }

    [Fact]
    public async Task ReturnNotFoundWhenEditingAnUnknownAmenity()
    {
        await using var dbContext = database.CreateDbContext();
        var result = await new AmenityService(dbContext).EditAsync(new AmenityRequest.Edit
        {
            AmenityId = 42,
            Names = new() { ["en"] = "Wi-Fi" },
            Icon = "fa-wifi",
        });

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task KeepTheAssignmentsWhenDeactivatingAndReactivating()
    {
        var wifi = Wifi();
        var deluxe = new RoomType("Deluxe Room");
        deluxe.AddAmenity(wifi);
        await ArrangeAsync(deluxe);

        await using (var dbContext = database.CreateDbContext())
        {
            (await new AmenityService(dbContext).DeactivateAsync(wifi.Id)).IsSuccess.ShouldBeTrue();
        }

        await using (var assertContext = database.CreateDbContext())
        {
            var saved = await assertContext.RoomTypes.Include(t => t.Amenities).SingleAsync();
            saved.Amenities.ShouldHaveSingleItem().IsActive.ShouldBeFalse();
        }

        await using (var dbContext = database.CreateDbContext())
        {
            (await new AmenityService(dbContext).ActivateAsync(wifi.Id)).IsSuccess.ShouldBeTrue();
        }

        await using var finalContext = database.CreateDbContext();
        var reactivated = await finalContext.RoomTypes.Include(t => t.Amenities).SingleAsync();
        reactivated.Amenities.ShouldHaveSingleItem().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task ListActiveAndInactiveAmenitiesByNameWithTheirRoomTypes()
    {
        var wifi = Wifi();
        var bathtub = new Amenity("Bathtub", "fa-bath");
        bathtub.Deactivate();
        var deluxe = new RoomType("Deluxe Room");
        var standard = new RoomType("Standard Room");
        deluxe.AddAmenity(wifi);
        standard.AddAmenity(wifi);
        await ArrangeAsync(deluxe, standard, bathtub);

        await using var dbContext = database.CreateDbContext();
        var result = await new AmenityService(dbContext).GetIndexAsync();

        var amenities = result.Value.Amenities;
        amenities.Select(a => a.Names["en"]).ShouldBe(["Bathtub", "Wi-Fi"]);
        amenities[0].IsActive.ShouldBeFalse();
        amenities[0].RoomTypeNames.ShouldBeEmpty();
        amenities[1].Names["nl"].ShouldBe("Wifi");
        amenities[1].RoomTypeNames.ShouldBe(["Deluxe Room", "Standard Room"]);
    }
}
