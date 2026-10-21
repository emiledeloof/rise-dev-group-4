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
            Description = "Flexible cancellation",
            RoomName = "Single Room",
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Standard");
        result.Value.RoomName.ShouldBe("Single Room");
        result.Value.PricePerNight.ShouldBe(79m);
        result.Value.Description.ShouldBe("Flexible cancellation");

        var savedRate = await dbContext.Rates.SingleAsync();
        savedRate.Room.Id.ShouldBe(room.Id);
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
}
