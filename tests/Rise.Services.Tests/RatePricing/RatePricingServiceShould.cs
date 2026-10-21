using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.RatePricing;
using Rise.Shared.RatePricing;

namespace Rise.Services.Tests.RatePricing;

/// <summary>
/// tests for <see cref="RatePricingService"/>
/// same setup as RoomServiceShould, an in memory db filled for each test
/// </summary>
public class RatePricingServiceShould
{
    /// <summary>
    /// fresh db with 2 rooms, the deluxe one is added first on purpose to check the sort
    /// each test gives its own db name so the tests dont share data
    /// </summary>
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string databaseName, Action<Rate>? tweak = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var deluxe = new Room("102", "Deluxe Room", 2);
        deluxe.AddRate("Non-refundable", 175m);

        var standard = new Room("101", "Standard Room", 2);
        var standardRate = standard.AddRate("Non-refundable", 133m);

        // lets a test change the rate before saving, eg mark it as deleted
        tweak?.Invoke(standardRate);

        dbContext.Rooms.AddRange(deluxe, standard);
        await dbContext.SaveChangesAsync();

        return dbContext;
    }

    // id of the rate of a room, looked up by room name so the tests dont depend on generated ids
    private static Task<int> RateIdOfAsync(ApplicationDbContext dbContext, string roomName)
        => dbContext.Rates.Where(r => r.Room.Name == roomName).Select(r => r.Id).SingleAsync();

    /// <summary>
    /// every rate comes back with its room name, sorted by room name
    /// </summary>
    [Fact]
    public async Task ListTheRatesSortedByRoom()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ListTheRatesSortedByRoom));

        // act
        var result = await new RatePricingService(dbContext).GetIndexAsync();

        // assert
        result.IsSuccess.ShouldBeTrue();
        var rates = result.Value.Rates.ToList();
        rates.Select(r => r.RoomName).ShouldBe(new[] { "Deluxe Room", "Standard Room" });
        rates[1].RateName.ShouldBe("Non-refundable");
        rates[1].PricePerNight.ShouldBe(133m);
    }

    /// <summary>
    /// a deleted rate must not show up on the admin page
    /// </summary>
    [Fact]
    public async Task HideDeletedRates()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(HideDeletedRates),
            rate => rate.IsDeleted = true);

        // act
        var result = await new RatePricingService(dbContext).GetIndexAsync();

        // assert
        result.Value.Rates.Select(r => r.RoomName).ShouldBe(new[] { "Deluxe Room" });
    }

    /// <summary>
    /// the new price is saved in the db
    /// </summary>
    [Fact]
    public async Task SaveTheNewPrice()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(SaveTheNewPrice));
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        var result = await new RatePricingService(dbContext)
            .EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m });

        // assert, read the rate again from the db to be sure it was saved
        result.IsSuccess.ShouldBeTrue();
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.PricePerNight.ShouldBe(140m);
    }

    /// <summary>
    /// an id that does not exist gives NotFound, nothing is changed
    /// </summary>
    [Fact]
    public async Task ReturnNotFoundForAnUnknownRate()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForAnUnknownRate));

        // act
        var result = await new RatePricingService(dbContext)
            .EditAsync(new RatePricingRequest.Edit { RateId = 999, PricePerNight = 140m });

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    /// <summary>
    /// a deleted rate cant be edited, same answer as an unknown one
    /// </summary>
    [Fact]
    public async Task ReturnNotFoundForADeletedRate()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForADeletedRate),
            rate => rate.IsDeleted = true);
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        var result = await new RatePricingService(dbContext)
            .EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m });

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }
}
