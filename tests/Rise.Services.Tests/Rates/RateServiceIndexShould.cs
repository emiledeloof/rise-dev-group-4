using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Rates;
using Rise.Shared.Rates;

namespace Rise.Services.Tests.Rates;

public class RateServiceIndexShould
{
    private const string DoubleRoom = "Double Room";
    private const string SingleRoom = "Single Room";

    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var doubleRoom = new Room("101", DoubleRoom, 2, new RoomType("Standard"));
        doubleRoom.AddRate("Flexible", 120m, "Flexible cancellation");
        // * new rates are refundable by default, this one is not
        var nonRefundable = doubleRoom.AddRate("Non-refundable", 90m, "No refund");
        nonRefundable.CancellationPolicy = CancellationPolicy.NonRefundable;
        nonRefundable.MinNights = 2;
        nonRefundable.IsActive = false;
        doubleRoom.AddRate("Deleted rate", 70m, "Archived policy").IsDeleted = true;

        var singleRoom = new Room("102", SingleRoom, 1, new RoomType("Standard"));
        singleRoom.AddRate("Standard", 75m);

        var deletedRoom = new Room("103", "Deleted Room", 2, new RoomType("Standard")) { IsDeleted = true };
        deletedRoom.AddRate("Archived", 50m, "Deleted room policy");

        dbContext.Rooms.AddRange(doubleRoom, singleRoom, deletedRoom);
        await dbContext.SaveChangesAsync();

        return dbContext;
    }

    [Fact]
    public async Task FilterRatesByRoomName()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FilterRatesByRoomName));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            RoomName = DoubleRoom,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Rates.Select(rate => rate.RoomName).Distinct().ShouldBe([DoubleRoom]);
    }

    [Fact]
    public async Task FilterByDateRangeAndKeepOverlappingRatesForTheSameRoom()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(FilterByDateRangeAndKeepOverlappingRatesForTheSameRoom))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var room = new Room("101", DoubleRoom, 2, new RoomType("Standard"));
        room.AddRate("Autumn", 120m, validFrom: new DateOnly(2026, 10, 1), validTo: new DateOnly(2026, 10, 31));
        room.AddRate("Autumn special", 100m, validFrom: new DateOnly(2026, 10, 1), validTo: new DateOnly(2026, 10, 31));
        room.AddRate("Any dates", 90m);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 31),
            FilterByDateRange = true,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Rates.Select(rate => rate.Name).OrderBy(name => name)
            .ShouldBe(["Autumn", "Autumn special"]);
        result.Value.DateRanges.ShouldContain("2026-10-01 - 2026-10-31");
    }

    [Fact]
    public async Task FilterDateRangesWithAnOpenEndExactly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(FilterDateRangesWithAnOpenEndExactly))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var room = new Room("101", DoubleRoom, 2, new RoomType("Standard"));
        room.AddRate("Until October", 120m, validTo: new DateOnly(2026, 10, 31));
        room.AddRate("From October", 100m, validFrom: new DateOnly(2026, 10, 1));
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            EndDate = new DateOnly(2026, 10, 31),
            FilterByDateRange = true,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Select(rate => rate.Name).ShouldBe(["Until October"]);
    }

    /// <summary>
    /// the policy filter uses the real cancellation policy, not the description
    /// </summary>
    [Fact]
    public async Task FilterRatesByCancellationPolicy()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FilterRatesByCancellationPolicy));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            Policy = RatePolicies.NonRefundable,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Rates.Single().Name.ShouldBe("Non-refundable");
    }

    [Fact]
    public async Task ApplyRoomAndPolicyFiltersTogether()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ApplyRoomAndPolicyFiltersTogether));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            RoomName = DoubleRoom,
            Policy = RatePolicies.NonRefundable,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Rates.Single().Name.ShouldBe("Non-refundable");
    }

    [Fact]
    public async Task ReturnOnlyNonDeletedRatesAndAllFilterOptions()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnOnlyNonDeletedRatesAndAllFilterOptions));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            RoomName = SingleRoom,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Select(rate => rate.Name).ShouldBe(["Standard"]);
        result.Value.TotalCount.ShouldBe(1);

        // Filter choices should remain complete even while the current results are filtered.
        result.Value.RoomNames.ShouldBe([DoubleRoom, SingleRoom]);
        result.Value.Policies.ShouldBe([RatePolicies.NonRefundable, RatePolicies.Refundable]);
    }

    [Fact]
    public async Task KeepTotalCountIndependentOfTheCurrentPage()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepTotalCountIndependentOfTheCurrentPage));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            Take = 1,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Count().ShouldBe(1);
        result.Value.TotalCount.ShouldBe(3);
    }

    /// <summary>
    /// refundable rates can be filtered too
    /// </summary>
    [Fact]
    public async Task FilterRefundableRates()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FilterRefundableRates));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            Policy = RatePolicies.Refundable,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Select(rate => rate.Name).OrderBy(name => name).ShouldBe(["Flexible", "Standard"]);
    }

    /// <summary>
    /// each row has what the edit form needs, min nights and the policy values
    /// </summary>
    [Fact]
    public async Task ReturnTheMinNightsAndThePolicyOfEachRate()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheMinNightsAndThePolicyOfEachRate));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index());

        var nonRefundable = result.Value.Rates.Single(rate => rate.Name == "Non-refundable");
        nonRefundable.MinNights.ShouldBe(2);
        nonRefundable.IsRefundable.ShouldBeFalse();
        nonRefundable.IsActive.ShouldBeFalse();

        var flexible = result.Value.Rates.Single(rate => rate.Name == "Flexible");
        flexible.MinNights.ShouldBe(1);
        flexible.IsRefundable.ShouldBeTrue();
        flexible.FullRefundDays.ShouldBe(30);
        flexible.PartialRefundDays.ShouldBe(7);
        flexible.PartialRefundPercent.ShouldBe(50);
        flexible.IsActive.ShouldBeTrue();
    }
}
