using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Services.RatePricing;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;

namespace Rise.Services.IntegrationTests.RatePricing;

/// <summary>Tests the save of the edit form of the rates page against a real postgres database.</summary>
[Collection(DatabaseCollection.Name)]
public class RatePricingServiceShould(PostgresFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // 2 rooms, the deluxe one is added first on purpose, returns the id of the standard rate
    private async Task<int> SeedAsync(Action<Rate>? tweak = null)
    {
        await using var dbContext = database.CreateDbContext();
        dbContext.GeneralSettings.Add(new GeneralSettings(false, 0m));

        var deluxe = new Room("102", "Deluxe Room", 2);
        deluxe.AddRate("Non-refundable", 175m);

        var standard = new Room("101", "Standard Room", 2);
        var standardRate = standard.AddRate("Non-refundable", 133m);

        // lets a test change the rate before saving, for example mark it as deleted
        tweak?.Invoke(standardRate);

        dbContext.Rooms.AddRange(deluxe, standard);
        await dbContext.SaveChangesAsync();
        return standardRate.Id;
    }

    // calls the service with its own context, so the assert cannot read a tracked object
    private async Task<Result<T>> ActAsync<T>(Func<RatePricingService, Task<Result<T>>> action)
    {
        await using var dbContext = database.CreateDbContext();
        return await action(new RatePricingService(dbContext));
    }

    // EditAsync returns a result without value, so it gets its own helper
    private async Task<Result> EditAsync(RatePricingRequest.Edit request)
    {
        await using var dbContext = database.CreateDbContext();
        return await new RatePricingService(dbContext).EditAsync(request);
    }

    private async Task<Rate> ReadRateAsync(int rateId)
    {
        await using var dbContext = database.CreateDbContext();
        return await dbContext.Rates.SingleAsync(r => r.Id == rateId);
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

    [Fact]
    public async Task SaveTheNewPrice()
    {
        var rateId = await SeedAsync();

        var result = await EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m, MinNights = 1 });

        result.IsSuccess.ShouldBeTrue();
        (await ReadRateAsync(rateId)).PricePerNight.ShouldBe(140m);
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownRate()
    {
        await SeedAsync();

        var result = await EditAsync(new RatePricingRequest.Edit { RateId = 999, PricePerNight = 140m, MinNights = 1 });

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    // a deleted rate cannot be edited, same answer as an unknown one
    [Fact]
    public async Task ReturnNotFoundForADeletedRate()
    {
        var rateId = await SeedAsync(rate => rate.IsDeleted = true);

        var result = await EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 140m, MinNights = 1 });

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task SaveTheNewMinNights()
    {
        var rateId = await SeedAsync();

        var result = await EditAsync(new RatePricingRequest.Edit { RateId = rateId, PricePerNight = 133m, MinNights = 3 });

        result.IsSuccess.ShouldBeTrue();
        (await ReadRateAsync(rateId)).MinNights.ShouldBe(3);
    }

    [Fact]
    public async Task SaveActiveStatus()
    {
        var rateId = await SeedAsync();

        var result = await EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            IsActive = false,
        });

        result.IsSuccess.ShouldBeTrue();
        (await ReadRateAsync(rateId)).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ReadAndUpdateGlobalVatSettings()
    {
        await SeedAsync();

        var initial = await ActAsync(s => s.GetGeneralSettingsAsync());
        initial.Value.IsVatEnabled.ShouldBeFalse();
        initial.Value.VatPercentage.ShouldBe(0m);

        var updated = await ActAsync(s => s.UpdateGeneralSettingsAsync(new GeneralSettingsRequest.Update
        {
            IsVatEnabled = true,
            VatPercentage = 12m,
        }));

        updated.IsSuccess.ShouldBeTrue();
        updated.Value.IsVatEnabled.ShouldBeTrue();
        updated.Value.VatPercentage.ShouldBe(12m);
        (await ActAsync(s => s.GetGeneralSettingsAsync())).Value.VatPercentage.ShouldBe(12m);
    }

    [Fact]
    public async Task SaveAvailabilityDateRange()
    {
        var rateId = await SeedAsync();
        var startDate = new DateOnly(2026, 10, 1);
        var endDate = new DateOnly(2026, 10, 31);

        var result = await EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            StartDate = startDate,
            EndDate = endDate,
        });

        result.IsSuccess.ShouldBeTrue();
        var saved = await ReadRateAsync(rateId);
        saved.StartDate.ShouldBe(startDate);
        saved.EndDate.ShouldBe(endDate);
    }

    [Fact]
    public async Task SaveARefundablePolicyWithItsValues()
    {
        var rateId = await SeedAsync(rate => rate.CancellationPolicy = CancellationPolicy.NonRefundable);

        var result = await EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            IsRefundable = true,
            FullRefundDays = 45,
            PartialRefundDays = 10,
            PartialRefundPercent = 30
        });

        result.IsSuccess.ShouldBeTrue();
        var saved = await ReadRateAsync(rateId);
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        saved.FullRefundDays.ShouldBe(45);
        saved.PartialRefundDays.ShouldBe(10);
        saved.PartialRefundPercent.ShouldBe(30);
    }

    // ! a non refundable rate keeps its old refund values even if the request sends invalid ones
    [Fact]
    public async Task KeepTheOldRefundValuesForANonRefundableRate()
    {
        // the rate starts refundable 30, 7, 50
        var rateId = await SeedAsync();

        var result = await EditAsync(new RatePricingRequest.Edit
        {
            RateId = rateId,
            PricePerNight = 133m,
            MinNights = 1,
            IsRefundable = false,
            FullRefundDays = -5,
            PartialRefundDays = -1,
            PartialRefundPercent = 150
        });

        result.IsSuccess.ShouldBeTrue();
        var saved = await ReadRateAsync(rateId);
        saved.CancellationPolicy.ShouldBe(CancellationPolicy.NonRefundable);
        saved.FullRefundDays.ShouldBe(30);
        saved.PartialRefundDays.ShouldBe(7);
        saved.PartialRefundPercent.ShouldBe(50);
    }

    [Fact]
    public async Task RenameANonRefundableRateWhenItBecomesRefundable()
    {
        var rateId = await SeedAsync(rate =>
        {
            rate.CancellationPolicy = CancellationPolicy.NonRefundable;
            rate.Description = "No refund after booking";
        });

        await EditAsync(PolicyRequest(rateId, isRefundable: true));

        var saved = await ReadRateAsync(rateId);
        saved.Name.ShouldBe("Refundable");
        saved.Description.ShouldBeNull();
    }

    [Fact]
    public async Task RenameARefundableRateWhenItBecomesNonRefundable()
    {
        var rateId = await SeedAsync(rate => rate.Name = "Refundable");

        await EditAsync(PolicyRequest(rateId, isRefundable: false));

        var saved = await ReadRateAsync(rateId);
        saved.Name.ShouldBe("Non-refundable");
        saved.Description.ShouldBe("No refund after booking");
    }

    // ! a custom name is never changed by the policy
    [Fact]
    public async Task KeepACustomNameWhenThePolicyChanges()
    {
        var rateId = await SeedAsync(rate =>
        {
            rate.Name = "Breakfast included";
            rate.CancellationPolicy = CancellationPolicy.NonRefundable;
        });

        await EditAsync(PolicyRequest(rateId, isRefundable: true));

        (await ReadRateAsync(rateId)).Name.ShouldBe("Breakfast included");
    }
}
