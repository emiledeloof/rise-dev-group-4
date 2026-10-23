using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Server.Endpoints.Rates;
using Rise.Services.Rates;
using Rise.Shared.Rates;

namespace Rise.Services.Tests.Rates;

public class RateCreateEndpointShould
{
    [Fact]
    public async Task PassTheCreateRequestToTheRateService()
    {
        var rateService = Substitute.For<IRateService>();
        var request = new RateRequest.Create
        {
            Name = "Standard",
            RoomName = "Deluxe Room",
            PricePerNight = 79m,
            Description = "Flexible cancellation",
        };
        rateService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RateResponse.Create
            {
                Id = 3,
                Name = request.Name,
                RoomName = request.RoomName,
                PricePerNight = request.PricePerNight,
                Description = request.Description,
            }));

        var result = await new Create(rateService).ExecuteAsync(request, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Standard");
        result.Value.RoomName.ShouldBe("Deluxe Room");
        await rateService.Received(1).CreateAsync(request, CancellationToken.None);
    }

    [Fact]
    public async Task ReturnAServiceFailureWithoutHidingItsReason()
    {
        var rateService = Substitute.For<IRateService>();
        var request = new RateRequest.Create
        {
            Name = "Standard",
            RoomName = "Missing room",
            PricePerNight = 79m,
            Description = "Flexible cancellation",
        };
        rateService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result.NotFound("Room not found."));

        var result = await new Create(rateService).ExecuteAsync(request, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Errors.ShouldContain("Room not found.");
    }

    [Fact]
    public async Task PersistTheRateWhenTheEndpointRunsWithTheRealService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(PersistTheRateWhenTheEndpointRunsWithTheRealService))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var room = new Room("101", "Deluxe Room", 2);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var endpoint = new Create(new RateService(dbContext));
        var request = new RateRequest.Create
        {
            Name = "Standard",
            RoomName = room.Name,
            PricePerNight = 79m,
            Description = "Flexible cancellation",
        };

        var result = await endpoint.ExecuteAsync(request, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var savedRate = await dbContext.Rates.SingleAsync();
        savedRate.Name.ShouldBe("Standard");
        savedRate.PricePerNight.ShouldBe(79m);
        savedRate.Description.ShouldBe("Flexible cancellation");
        savedRate.Room.Id.ShouldBe(room.Id);
    }
}
