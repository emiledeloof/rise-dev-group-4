using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Rates;
using Rise.Shared.Rates;

namespace Rise.Services.Tests.Rates;

public class RateServiceCreateShould
{
    [Fact]
    public async Task CreateRateForAnExistingRoom()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(CreateRateForAnExistingRoom))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var room = new Room("101", "Single Room", 1);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var result = await new RateService(dbContext).CreateAsync(new RateRequest.Create
        {
            Name = "Standard",
            PricePerNight = 79m,
            IsActive = false,
            Description = "Flexible cancellation",
            RoomName = "Single Room",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 31),
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Standard");
        result.Value.RoomName.ShouldBe("Single Room");
        result.Value.PricePerNight.ShouldBe(79m);
        result.Value.IsActive.ShouldBeFalse();
        result.Value.Description.ShouldBe("Flexible cancellation");
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 10, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 10, 31));

        var savedRate = await dbContext.Rates.SingleAsync();
        savedRate.Room.Id.ShouldBe(room.Id);
        savedRate.StartDate.ShouldBe(new DateOnly(2026, 10, 1));
        savedRate.EndDate.ShouldBe(new DateOnly(2026, 10, 31));
        savedRate.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnNotFoundWhenCreatingRateForAnUnknownRoom()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(ReturnNotFoundWhenCreatingRateForAnUnknownRoom))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var result = await new RateService(dbContext).CreateAsync(new RateRequest.Create
        {
            Name = "Standard",
            PricePerNight = 79m,
            RoomName = "Missing Room",
        });

        result.Status.ShouldBe(ResultStatus.NotFound);
        (await dbContext.Rates.CountAsync()).ShouldBe(0);
    }

    // empty db with 1 room, each test gives its own db name
    private static async Task<ApplicationDbContext> CreateDbWithARoomAsync(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var dbContext = new ApplicationDbContext(options);
        dbContext.Rooms.Add(new Room("101", "Single Room", 1));
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    /// <summary>
    /// ! without the box checked the new rate is non refundable, even if the domain default is refundable
    /// </summary>
    [Fact]
    public async Task CreateANonRefundableRateByDefault()
    {
        await using var dbContext = await CreateDbWithARoomAsync(nameof(CreateANonRefundableRateByDefault));

        await new RateService(dbContext).CreateAsync(new RateRequest.Create
        {
            Name = "Standard",
            PricePerNight = 79m,
            RoomName = "Single Room",
            MinNights = 2,
        });

        var saved = await dbContext.Rates.SingleAsync();
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.NonRefundable);
        saved.MinNights.ShouldBe(2);
    }

    /// <summary>
    /// a refundable rate is saved with its refund values
    /// </summary>
    [Fact]
    public async Task CreateARefundableRateWithItsValues()
    {
        await using var dbContext = await CreateDbWithARoomAsync(nameof(CreateARefundableRateWithItsValues));

        await new RateService(dbContext).CreateAsync(new RateRequest.Create
        {
            Name = "Flexible",
            PricePerNight = 99m,
            RoomName = "Single Room",
            IsRefundable = true,
            FullRefundDays = 45,
            PartialRefundDays = 10,
            PartialRefundPercent = 30,
        });

        var saved = await dbContext.Rates.SingleAsync();
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        saved.FullRefundDays.ShouldBe(45);
        saved.PartialRefundDays.ShouldBe(10);
        saved.PartialRefundPercent.ShouldBe(30);
    }
}
