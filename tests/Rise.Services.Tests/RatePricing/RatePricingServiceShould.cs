using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.RatePricing;
using Rise.Shared.RatePricing;

namespace Rise.Services.Tests.RatePricing;

/// <summary>
/// tests for <see cref="RatePricingService"/>, the save of the edit form of the rates page
/// the list is tested in Rates/RateServiceIndexShould
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
            .EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m, MinNights = 1 });

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
            .EditAsync(new RatePricingRequest.Edit { RateId = 999, PricePerNight = 140m, MinNights = 1 });

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
            .EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m, MinNights = 1 });

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    /// <summary>
    /// the new min nb of nights is saved in the db
    /// </summary>
    [Fact]
    public async Task SaveTheNewMinNights()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(SaveTheNewMinNights));
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        var result = await new RatePricingService(dbContext)
            .EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 133m, MinNights = 3 });

        // assert, read the rate again from the db to be sure it was saved
        result.IsSuccess.ShouldBeTrue();
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.MinNights.ShouldBe(3);
    }

    /// <summary>
    /// a refundable policy is saved with its new values
    /// </summary>
    [Fact]
    public async Task SaveARefundablePolicyWithItsValues()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(SaveARefundablePolicyWithItsValues),
            rate => rate.CancellationPolicy = CancellationPolicy.NonRefundable);
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        var result = await new RatePricingService(dbContext).EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            IsRefundable = true,
            FullRefundDays = 45,
            PartialRefundDays = 10,
            PartialRefundPercent = 30
        });

        // assert, read the rate again from the db to be sure it was saved
        result.IsSuccess.ShouldBeTrue();
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        saved.FullRefundDays.ShouldBe(45);
        saved.PartialRefundDays.ShouldBe(10);
        saved.PartialRefundPercent.ShouldBe(30);
    }

    /// <summary>
    /// ! a non refundable rate keeps its old refund values, even if the request sends invalid ones
    /// without the if in the service the Guard would throw here
    /// </summary>
    [Fact]
    public async Task KeepTheOldRefundValuesForANonRefundableRate()
    {
        // arrange, the rate starts refundable 30/7/50
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepTheOldRefundValuesForANonRefundableRate));
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        var result = await new RatePricingService(dbContext).EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            IsRefundable = false,
            FullRefundDays = -5,
            PartialRefundDays = -1,
            PartialRefundPercent = 150
        });

        // assert
        result.IsSuccess.ShouldBeTrue();
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.NonRefundable);
        saved.FullRefundDays.ShouldBe(30);
        saved.PartialRefundDays.ShouldBe(7);
        saved.PartialRefundPercent.ShouldBe(50);
    }

    // request that only changes the policy, the other values stay the same as the seed
    private static RatePricingRequest.Edit PolicyRequest(int rateId, bool isRefundable) => new()
    {
        RateId = rateId,
        PricePerNight = 133m,
        MinNights = 1,
        IsRefundable = isRefundable,
        FullRefundDays = 30,
        PartialRefundDays = 7,
        PartialRefundPercent = 50
    };

    /// <summary>
    /// a rate named Non-refundable is renamed Refundable when it becomes refundable
    /// its no refund conditions are removed, the guest sees the refund rules instead
    /// </summary>
    [Fact]
    public async Task RenameANonRefundableRateWhenItBecomesRefundable()
    {
        // arrange, the seeded rate is named Non-refundable
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RenameANonRefundableRateWhenItBecomesRefundable), rate =>
        {
            rate.CancellationPolicy = CancellationPolicy.NonRefundable;
            rate.Description = "No refund after booking";
        });
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        await new RatePricingService(dbContext).EditAsync(PolicyRequest(rateId, isRefundable: true));

        // assert
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.Name.ShouldBe("Refundable");
        saved.Description.ShouldBeNull();
    }

    /// <summary>
    /// and back, a rate named Refundable is renamed Non-refundable with its conditions
    /// </summary>
    [Fact]
    public async Task RenameARefundableRateWhenItBecomesNonRefundable()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RenameARefundableRateWhenItBecomesNonRefundable),
            rate => rate.Name = "Refundable");
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        await new RatePricingService(dbContext).EditAsync(PolicyRequest(rateId, isRefundable: false));

        // assert
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.Name.ShouldBe("Non-refundable");
        saved.Description.ShouldBe("No refund after booking");
    }

    /// <summary>
    /// ! a custom name is never changed by the policy
    /// </summary>
    [Fact]
    public async Task KeepACustomNameWhenThePolicyChanges()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepACustomNameWhenThePolicyChanges), rate =>
        {
            rate.Name = "Breakfast included";
            rate.CancellationPolicy = CancellationPolicy.NonRefundable;
        });
        var rateId = await RateIdOfAsync(dbContext, "Standard Room");

        // act
        await new RatePricingService(dbContext).EditAsync(PolicyRequest(rateId, isRefundable: true));

        // assert
        var saved = await dbContext.Rates.AsNoTracking().SingleAsync(r => r.Id == rateId);
        saved.Name.ShouldBe("Breakfast included");
    }
}
