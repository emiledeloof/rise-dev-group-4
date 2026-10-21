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

        var doubleRoom = new Room("101", DoubleRoom, 2);
        doubleRoom.AddRate("Flexible", 120m, "Flexible cancellation");
        doubleRoom.AddRate("Non-refundable", 90m, "No refund");
        doubleRoom.AddRate("Deleted rate", 70m, "Archived policy").IsDeleted = true;

        var singleRoom = new Room("102", SingleRoom, 1);
        singleRoom.AddRate("Standard", 75m);

        var deletedRoom = new Room("103", "Deleted Room", 2) { IsDeleted = true };
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
    public async Task FilterRatesByPolicyDescription()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FilterRatesByPolicyDescription));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            Policy = "No refund",
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
            Policy = "No refund",
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Rates.Single().Name.ShouldBe("Non-refundable");
    }

    [Fact]
    public async Task ReturnOnlyActiveRatesAndAllFilterOptions()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnOnlyActiveRatesAndAllFilterOptions));

        var result = await new RateService(dbContext).GetIndexAsync(new RateRequest.Index
        {
            RoomName = SingleRoom,
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Select(rate => rate.Name).ShouldBe(["Standard"]);
        result.Value.TotalCount.ShouldBe(1);

        // Filter choices should remain complete even while the current results are filtered.
        result.Value.RoomNames.ShouldBe([DoubleRoom, SingleRoom]);
        result.Value.Policies.ShouldBe(["Flexible cancellation", "No refund"]);
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
}
