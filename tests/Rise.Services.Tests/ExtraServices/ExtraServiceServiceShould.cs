using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Services.ExtraServices;
using Rise.Shared.ExtraServices;
using Shouldly;
using Xunit;
using DomainCadence = Rise.Domain.ExtraServices.ChargeCadence;
using DomainExtraService = Rise.Domain.ExtraServices.ExtraService;

namespace Rise.Services.Tests.ExtraServices;

public class ExtraServiceServiceShould
{
    private static ApplicationDbContext NewDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateExtraServiceSuccessfully()
    {
        await using var dbContext = NewDbContext(nameof(CreateExtraServiceSuccessfully));
        var service = new ExtraServiceService(dbContext);

        var result = await service.CreateAsync(new ExtraServiceRequest.Create
        {
            Name = "Breakfast Buffet",
            Price = 18m,
            Cadence = ChargeCadence.PerNight,
            IsPerPerson = true,
            Description = "Full English and continental buffet"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Breakfast Buffet");

        var saved = await dbContext.ExtraServices.SingleAsync();
        saved.Name.ShouldBe("Breakfast Buffet");
        saved.Price.ShouldBe(18m);
        saved.Cadence.ShouldBe(DomainCadence.PerNight);
        saved.IsPerPerson.ShouldBeTrue();
        saved.Description.ShouldBe("Full English and continental buffet");
        saved.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RefuseDuplicateServiceNameRegardlessOfCase()
    {
        await using var dbContext = NewDbContext(nameof(RefuseDuplicateServiceNameRegardlessOfCase));
        dbContext.ExtraServices.Add(new DomainExtraService("Breakfast", 15m));
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);

        var result = await service.CreateAsync(new ExtraServiceRequest.Create
        {
            Name = "  breakfast  ",
            Price = 20m
        });

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.ExtraServices.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task GetIndexReturnsAllServicesOrderedByName()
    {
        await using var dbContext = NewDbContext(nameof(GetIndexReturnsAllServicesOrderedByName));
        dbContext.ExtraServices.AddRange(
            new DomainExtraService("Parking", 10m, DomainCadence.PerNight, false),
            new DomainExtraService("Breakfast", 15m, DomainCadence.PerNight, true),
            new DomainExtraService("Cleaning", 40m, DomainCadence.Once, false));
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);
        var result = await service.GetIndexAsync();

        result.IsSuccess.ShouldBeTrue();
        var names = result.Value.Services.Select(s => s.Name).ToList();
        names.ShouldBe(new[] { "Breakfast", "Cleaning", "Parking" });
    }

    [Fact]
    public async Task FilterOnlyActiveServices()
    {
        await using var dbContext = NewDbContext(nameof(FilterOnlyActiveServices));
        var activeService = new DomainExtraService("Active Service", 10m);
        var inactiveService = new DomainExtraService("Inactive Service", 10m);
        inactiveService.Deactivate();

        dbContext.ExtraServices.AddRange(activeService, inactiveService);
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);
        var result = await service.GetIndexAsync(onlyActive: true);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Services.Count().ShouldBe(1);
        result.Value.Services.Single().Name.ShouldBe("Active Service");
    }

    [Fact]
    public async Task GetByIdReturnsServiceOrNotFound()
    {
        await using var dbContext = NewDbContext(nameof(GetByIdReturnsServiceOrNotFound));
        var entity = new DomainExtraService("Sauna", 20m);
        dbContext.ExtraServices.Add(entity);
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);

        var found = await service.GetByIdAsync(entity.Id);
        found.IsSuccess.ShouldBeTrue();
        found.Value.Name.ShouldBe("Sauna");

        var notFound = await service.GetByIdAsync(9999);
        notFound.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task EditExtraServiceSuccessfully()
    {
        await using var dbContext = NewDbContext(nameof(EditExtraServiceSuccessfully));
        var entity = new DomainExtraService("Bicycle", 10m, DomainCadence.PerNight, true);
        dbContext.ExtraServices.Add(entity);
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);

        var result = await service.EditAsync(new ExtraServiceRequest.Edit
        {
            Id = entity.Id,
            Name = "E-Bike Rental",
            Price = 25m,
            Cadence = ChargeCadence.Once,
            IsPerPerson = false,
            Description = "High-end electric bike",
            IsActive = true
        });

        result.IsSuccess.ShouldBeTrue();

        var updated = await dbContext.ExtraServices.FindAsync(entity.Id);
        updated!.Name.ShouldBe("E-Bike Rental");
        updated.Price.ShouldBe(25m);
        updated.Cadence.ShouldBe(DomainCadence.Once);
        updated.IsPerPerson.ShouldBeFalse();
        updated.Description.ShouldBe("High-end electric bike");
    }

    [Fact]
    public async Task ToggleActiveStatus()
    {
        await using var dbContext = NewDbContext(nameof(ToggleActiveStatus));
        var entity = new DomainExtraService("Airport Shuttle", 50m);
        dbContext.ExtraServices.Add(entity);
        await dbContext.SaveChangesAsync();

        var service = new ExtraServiceService(dbContext);

        // Active -> Inactive
        var result1 = await service.ToggleActiveAsync(entity.Id);
        result1.IsSuccess.ShouldBeTrue();
        (await dbContext.ExtraServices.FindAsync(entity.Id))!.IsActive.ShouldBeFalse();

        // Inactive -> Active
        var result2 = await service.ToggleActiveAsync(entity.Id);
        result2.IsSuccess.ShouldBeTrue();
        (await dbContext.ExtraServices.FindAsync(entity.Id))!.IsActive.ShouldBeTrue();
    }
}
