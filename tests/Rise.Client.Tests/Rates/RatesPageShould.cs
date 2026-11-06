using Ardalis.Result;
using Rise.Client.Search;
using Rise.Shared.Identity;
using Rise.Shared.RatePricing;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;
using Rise.Shared.Settings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Rates;

public class RatesPageShould : BunitContext
{
    private readonly FakeRateService _rateService = new();
    private readonly FakeRatePricingService _ratePricingService = new();

    public RatesPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRateService>(_rateService);
        Services.AddSingleton<IRoomService>(new FakeRoomService());
        Services.AddSingleton<IRatePricingService>(_ratePricingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void ShowRateDetailsAndFilterOptions()
    {
        var cut = Render<Rates>();

        var rows = cut.FindAll(".rate-table tbody tr");
        rows.Count.ShouldBe(2);
        rows[0].TextContent.ShouldContain("Non-refundable");
        rows[0].TextContent.ShouldContain("Double Room");
        rows[0].TextContent.ShouldContain("€90.00");

        var selects = cut.FindAll(".select-field select");
        selects.Count.ShouldBe(3);
        selects[0].QuerySelectorAll("option").Select(option => option.TextContent)
            .ShouldBe(["All rooms", "Double Room", "Single Room"]);
        selects[1].QuerySelectorAll("option").Select(option => option.TextContent)
            .ShouldBe(["All date ranges", "01 Oct 2026 - 31 Oct 2026"]);
        selects[2].QuerySelectorAll("option").Select(option => option.TextContent)
            .ShouldBe(["All policies", "Non-refundable", "Refundable"]);

        _rateService.IndexRequests.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(request =>
            {
                request.Skip.ShouldBe(0);
                request.Take.ShouldBe(10);
            });
    }

    [Fact]
    public void ShowVatInclusivePriceAndOriginalPriceWhenVatIsEnabled()
    {
        _ratePricingService.GeneralSettingsAnswer = Result.Success(new GeneralSettingsDto
        {
            IsVatEnabled = true,
            VatPercentage = 20m,
        });

        var cut = Render<Rates>();

        var priceCell = cut.Find(".rate-table tbody tr:first-child .rate-price");
        priceCell.QuerySelector("span")!.TextContent.ShouldBe("€108.00");
        priceCell.QuerySelector(".rate-price-before-vat")!.TextContent.ShouldBe("€90.00");

        cut.FindAll(".rate-edit-btn")[0].Click();
        cut.Find("#price").GetAttribute("value").ShouldBe("90");
    }

    [Fact]
    public void ShowOnlyOriginalPriceWhenVatIsDisabled()
    {
        _ratePricingService.GeneralSettingsAnswer = Result.Success(new GeneralSettingsDto
        {
            IsVatEnabled = false,
            VatPercentage = 20m,
        });

        var cut = Render<Rates>();

        var priceCell = cut.Find(".rate-table tbody tr:first-child .rate-price");
        priceCell.TextContent.Trim().ShouldBe("€90.00");
        priceCell.QuerySelector(".rate-price-before-vat").ShouldBeNull();
    }

    [Fact]
    public void ShowOnlyOriginalPriceWhenEnabledVatPercentageIsZero()
    {
        _ratePricingService.GeneralSettingsAnswer = Result.Success(new GeneralSettingsDto
        {
            IsVatEnabled = true,
            VatPercentage = 0m,
        });

        var cut = Render<Rates>();

        var priceCell = cut.Find(".rate-table tbody tr:first-child .rate-price");
        priceCell.TextContent.Trim().ShouldBe("€90.00");
        priceCell.QuerySelector(".rate-price-before-vat").ShouldBeNull();
    }

    [Fact]
    public async Task SendSelectedFilterAndResetToFirstPage()
    {
        _rateService.IndexAnswer = Result.Success(FakeRateService.CreateIndexAnswer(totalCount: 25));
        var cut = Render<Rates>();

        await cut.Find(".pagination-next").ClickAsync();
        _rateService.IndexRequests[^1].Skip.ShouldBe(10);

        await cut.FindAll(".select-field select")[0].ChangeAsync("Double Room");

        var filteredRequest = _rateService.IndexRequests[^1];
        filteredRequest.RoomName.ShouldBe("Double Room");
        filteredRequest.Policy.ShouldBeNull();
        filteredRequest.Skip.ShouldBe(0);

        await cut.FindAll(".select-field select")[1].ChangeAsync("2026-10-01 - 2026-10-31");
        var dateFilteredRequest = _rateService.IndexRequests[^1];
        dateFilteredRequest.StartDate.ShouldBe(new DateOnly(2026, 10, 1));
        dateFilteredRequest.EndDate.ShouldBe(new DateOnly(2026, 10, 31));
        dateFilteredRequest.FilterByDateRange.ShouldBeTrue();
    }

    [Fact]
    public void ShowEmptyMessageWhenThereAreNoRates()
    {
        _rateService.IndexAnswer = Result.Success(new RateResponse.Index
        {
            Rates = [],
            RoomNames = [],
            Policies = [],
            DateRanges = [],
            TotalCount = 0,
        });

        var cut = Render<Rates>();

        cut.Markup.ShouldContain("No rates yet.");
        cut.FindAll(".rate-table tbody tr").ShouldBeEmpty();
    }

    [Fact]
    public void ShowAnErrorWhenRatesCouldNotBeLoaded()
    {
        _rateService.IndexAnswer = Result.Error("Service unavailable");

        var cut = Render<Rates>();

        cut.Find(".notification.is-danger").TextContent.ShouldContain("The rates could not be loaded.");
    }

    [Fact]
    public void ShowFieldValidationForAMissingRoomAndDoNotSendTheRequest()
    {
        var cut = Render<Rates>();
        cut.Find("button").Click();
        cut.FindAll(".rate-create-fields input")[0].Change("Standard");
        cut.Find("input[type='number']").Change("79");
        cut.FindAll(".rate-create-fields")[0].QuerySelectorAll("input")[2].Change("Flexible cancellation");

        cut.Find(".rate-create-panel form").Submit();

        cut.Find(".select-field .help").TextContent.ShouldContain("Select a room type.");
        _rateService.CreateRequests.ShouldBeEmpty();
    }

    [Fact]
    public void ShowTheApiReasonWhenRateCreationFails()
    {
        _rateService.CreateAnswer = Result.NotFound("Room not found.");
        var cut = Render<Rates>();
        OpenAndFillCreateForm(cut);

        cut.Find(".rate-create-panel form").Submit();

        cut.Find(".rate-create-panel .notification.is-danger").TextContent
            .ShouldContain("Room not found.");
    }

    [Fact]
    public void CreateRateAndShowItInTheAdminList()
    {
        _rateService.CreateAnswer = Result.Success(new RateResponse.Create
        {
            Id = 3,
            Name = "Standard",
            RoomName = "Deluxe Room",
            PricePerNight = 79m,
            IsActive = true,
            Description = "Flexible cancellation",
        });

        var cut = Render<Rates>();
        OpenAndFillCreateForm(cut);
        cut.Find(".rate-create-panel form").Submit();

        _rateService.CreateRequests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(request =>
        {
            request.Name.ShouldBe("Standard");
            request.RoomName.ShouldBe("Deluxe Room");
            request.PricePerNight.ShouldBe(79m);
            request.IsActive.ShouldBeTrue();
            request.Description.ShouldBe("Flexible cancellation");
        });
        cut.Find(".rate-table tbody").TextContent.ShouldContain("Standard");
        cut.Find(".rate-table tbody").TextContent.ShouldContain("Deluxe Room");
        cut.Find(".rate-table tbody").TextContent.ShouldContain("€79.00");
        cut.FindAll(".rate-create-panel").ShouldBeEmpty();
    }

    /// <summary>
    /// the min nights and the real policy fill the columns that were empty before the merge
    /// </summary>
    [Fact]
    public void ShowTheMinNightsAndTheCancellationPolicy()
    {
        var cut = Render<Rates>();

        var rows = cut.FindAll(".rate-table tbody tr");
        rows[0].TextContent.ShouldContain("2");
        rows[0].QuerySelector(".rate-policy-non")!.TextContent.ShouldBe("Non-refundable");
        rows[1].QuerySelector(".rate-policy-fully")!.TextContent.ShouldBe("100% up to 30 days, 50% up to 7 days");
    }

    /// <summary>
    /// edit opens the form prefilled with the values of the rate
    /// </summary>
    [Fact]
    public void OpenTheEditFormWithTheValuesOfTheRate()
    {
        var cut = Render<Rates>();

        cut.FindAll(".rate-edit-btn")[1].Click();

        cut.Find("#price").GetAttribute("value").ShouldBe("79");
        cut.Find("#min-nights").GetAttribute("value").ShouldBe("1");
        cut.Find("#is-refundable").HasAttribute("checked").ShouldBeTrue();
        cut.FindAll("#is-active").ShouldBeEmpty();
        cut.Find("#full-refund-days").GetAttribute("value").ShouldBe("30");
        cut.Find(".rate-edit-panel .drp-value").TextContent.ShouldBe("1 Oct 2026 – 31 Oct 2026");
    }

    /// <summary>
    /// the refund fields only show for a refundable rate
    /// </summary>
    [Fact]
    public void HideTheRefundFieldsForANonRefundableRate()
    {
        var cut = Render<Rates>();

        cut.FindAll(".rate-edit-btn")[0].Click();

        cut.FindAll("#full-refund-days").ShouldBeEmpty();
    }

    /// <summary>
    /// save sends the edit, closes the form, reloads the list and says what changed
    /// </summary>
    [Fact]
    public void SaveTheEditAndShowWhatChanged()
    {
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[1].Click();
        cut.Find("#price").Change("85");
        cut.Find("#min-nights").Change("3");

        cut.Find(".rate-edit-panel form").Submit();

        _ratePricingService.EditRequests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(request =>
        {
            request.RateId.ShouldBe(2);
            request.PricePerNight.ShouldBe(85m);
            request.IsActive.ShouldBeTrue();
            request.MinNights.ShouldBe(3);
        });
        cut.FindAll(".rate-edit-panel").ShouldBeEmpty();
        cut.Find(".rate-success").TextContent.ShouldBe("Saved: price and minimum nights.");
        // 1 load on open, 1 after the save
        _rateService.IndexRequests.Count.ShouldBe(2);
    }

    [Fact]
    public void EditAvailabilityDatesUsingTheCalendar()
    {
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[1].Click();
        cut.Find(".rate-edit-panel .drp-field").Click();
        cut.Find("[aria-label='3 October 2026']").Click();
        cut.Find("[aria-label='8 October 2026']").Click();

        cut.Find(".rate-edit-panel form").Submit();

        var edit = _ratePricingService.EditRequests.ShouldHaveSingleItem();
        edit.StartDate.ShouldBe(new DateOnly(2026, 10, 3));
        edit.EndDate.ShouldBe(new DateOnly(2026, 10, 8));
        cut.Find(".rate-success").TextContent.ShouldContain("availability dates");
    }

    [Fact]
    public void ClearAvailabilityDatesWhenEditingARate()
    {
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[1].Click();
        cut.FindAll(".rate-edit-panel .rate-date-picker button")[1].Click();

        cut.Find(".rate-edit-panel form").Submit();

        var edit = _ratePricingService.EditRequests.ShouldHaveSingleItem();
        edit.StartDate.ShouldBeNull();
        edit.EndDate.ShouldBeNull();
    }

    /// <summary>
    /// a failed save keeps the form open with an error
    /// </summary>
    [Fact]
    public void KeepTheEditFormOpenWhenTheSaveFails()
    {
        _ratePricingService.EditAnswer = Result.Error("Server error");
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[1].Click();

        cut.Find(".rate-edit-panel form").Submit();

        cut.Find(".rate-edit-panel .notification.is-danger").TextContent
            .ShouldContain("The changes could not be saved.");
    }

    [Fact]
    public void ToggleRateActiveStatusWithoutOpeningTheEditForm()
    {
        var cut = Render<Rates>();
        var inactiveRate = cut.Find("input[role='switch'][aria-label='Activate Non-refundable']");

        inactiveRate.Change(true);

        _ratePricingService.EditRequests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(request =>
        {
            request.RateId.ShouldBe(1);
            request.IsActive.ShouldBeTrue();
            request.PricePerNight.ShouldBe(90m);
        });
        cut.Find(".rate-table tbody tr").GetAttribute("class").ShouldBeNull();
        cut.FindAll(".rate-edit-panel").ShouldBeEmpty();
    }

    [Fact]
    public void KeepInactiveRateMutedWhenItsStatusCannotBeSaved()
    {
        _ratePricingService.EditAnswer = Result.Error("Server error");
        var cut = Render<Rates>();

        cut.Find("input[role='switch'][aria-label='Activate Non-refundable']").Change(true);

        cut.Find(".rate-table tbody tr").GetAttribute("class").ShouldBe("rate-inactive-row");
        cut.Find(".rate-success.notification.is-danger").TextContent
            .ShouldContain("The rate status could not be updated.");
    }

    /// <summary>
    /// ! invalid refund values are stopped by the validator, nothing is sent
    /// </summary>
    [Fact]
    public void NotSendAnEditWithAFullPeriodShorterThanThePartialOne()
    {
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[1].Click();
        cut.Find("#full-refund-days").Change("5");

        cut.Find(".rate-edit-panel form").Submit();

        cut.Find(".rate-edit-panel .help").TextContent.ShouldContain("The full refund period must be longer than the partial one.");
        _ratePricingService.EditRequests.ShouldBeEmpty();
    }

    /// <summary>
    /// one form at a time, opening create closes edit
    /// </summary>
    [Fact]
    public void CloseTheEditFormWhenCreateIsOpened()
    {
        var cut = Render<Rates>();
        cut.FindAll(".rate-edit-btn")[0].Click();

        cut.Find(".rate-page-heading button").Click();

        cut.FindAll(".rate-edit-panel").ShouldBeEmpty();
        cut.FindAll(".rate-create-panel").Count.ShouldBe(1);
    }

    /// <summary>
    /// ! a new rate is non refundable unless the admin checks the box
    /// </summary>
    [Fact]
    public void CreateANonRefundableRateByDefault()
    {
        _rateService.CreateAnswer = Result.Success(new RateResponse.Create
        {
            Id = 3,
            Name = "Standard",
            RoomName = "Deluxe Room",
            PricePerNight = 79m,
            IsActive = true,
            Description = "Flexible cancellation",
        });
        var cut = Render<Rates>();
        OpenAndFillCreateForm(cut);

        cut.FindAll("#create-full-refund-days").ShouldBeEmpty();
        cut.Find(".rate-create-panel form").Submit();

        var request = _rateService.CreateRequests.ShouldHaveSingleItem();
        request.IsRefundable.ShouldBeFalse();
        request.MinNights.ShouldBe(1);
    }

    /// <summary>
    /// checking the box shows the refund fields and sends them
    /// </summary>
    [Fact]
    public void CreateARefundableRateWhenTheBoxIsChecked()
    {
        _rateService.CreateAnswer = Result.Success(new RateResponse.Create
        {
            Id = 3,
            Name = "Standard",
            RoomName = "Deluxe Room",
            PricePerNight = 79m,
            IsActive = true,
            Description = "Flexible cancellation",
        });
        var cut = Render<Rates>();
        OpenAndFillCreateForm(cut);

        cut.Find("#create-is-refundable").Change(true);
        cut.Find("#create-full-refund-days").Change("45");
        cut.Find(".rate-create-panel form").Submit();

        var request = _rateService.CreateRequests.ShouldHaveSingleItem();
        request.IsRefundable.ShouldBeTrue();
        request.FullRefundDays.ShouldBe(45);
        request.PartialRefundDays.ShouldBe(7);
        request.PartialRefundPercent.ShouldBe(50);
    }

    [Fact]
    public void OpenGeneralSettingsWithSavedVatValuesAndNoVatColumn()
    {
        _ratePricingService.GeneralSettingsAnswer = Result.Success(new GeneralSettingsDto
        {
            IsVatEnabled = true,
            VatPercentage = 12m,
        });
        var cut = Render<Rates>();

        cut.FindAll(".rate-page-heading button")[1].Click();

        cut.Find("#general-vat-percentage").GetAttribute("value").ShouldBe("12");
        cut.Find("input[role='switch'][aria-label='Enable VAT']").HasAttribute("checked").ShouldBeTrue();
        cut.Find(".rate-vat-status").TextContent.ShouldBe("VAT active · 12%");
        cut.FindAll(".rate-table th").Select(header => header.TextContent).ShouldNotContain("VAT");
    }

    [Fact]
    public void SaveGlobalVatSettingsAndShowTheActiveStatus()
    {
        var cut = Render<Rates>();

        cut.FindAll(".rate-page-heading button")[1].Click();
        cut.Find("input[role='switch'][aria-label='Enable VAT']").Change(true);
        cut.Find("#general-vat-percentage").Change("21");
        cut.Find(".rate-settings-panel form").Submit();

        _ratePricingService.GeneralSettingsRequests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(request =>
        {
            request.IsVatEnabled.ShouldBeTrue();
            request.VatPercentage.ShouldBe(21m);
        });
        cut.Find(".rate-vat-status").TextContent.ShouldBe("VAT active · 21%");
        cut.FindAll(".rate-settings-panel").ShouldBeEmpty();
    }

    private static void OpenAndFillCreateForm(IRenderedComponent<Rates> cut)
    {
        cut.Find(".rate-page-heading button").Click();
        cut.FindAll(".rate-create-fields input")[0].Change("Standard");
        cut.Find("input[type='number']").Change("79");
        cut.FindAll(".rate-create-fields")[0].QuerySelectorAll("input")[2].Change("Flexible cancellation");
        cut.Find(".rate-create-fields select").Change("Deluxe Room");
    }
}
