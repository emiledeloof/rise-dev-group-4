using Ardalis.Result;
using Rise.Client.Search;
using Rise.Shared.Identity;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Rates;

public class RatesPageShould : BunitContext
{
    private readonly FakeRateService _rateService = new();

    public RatesPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRateService>(_rateService);
        Services.AddSingleton<IRoomService>(new FakeRoomService());
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

        var selects = cut.FindAll(".rate-filter select");
        selects.Count.ShouldBe(2);
        selects[0].QuerySelectorAll("option").Select(option => option.TextContent)
            .ShouldBe(["All rooms", "Double Room", "Single Room"]);
        selects[1].QuerySelectorAll("option").Select(option => option.TextContent)
            .ShouldBe(["All policies", "No refund"]);

        _rateService.IndexRequests.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(request =>
            {
                request.Skip.ShouldBe(0);
                request.Take.ShouldBe(10);
            });
    }

    [Fact]
    public async Task SendSelectedFilterAndResetToFirstPage()
    {
        _rateService.IndexAnswer = Result.Success(FakeRateService.CreateIndexAnswer(totalCount: 25));
        var cut = Render<Rates>();

        await cut.Find(".pagination-next").ClickAsync();
        _rateService.IndexRequests[^1].Skip.ShouldBe(10);

        await cut.FindAll(".rate-filter select")[0].ChangeAsync("Double Room");

        var filteredRequest = _rateService.IndexRequests[^1];
        filteredRequest.RoomName.ShouldBe("Double Room");
        filteredRequest.Policy.ShouldBeNull();
        filteredRequest.Skip.ShouldBe(0);
    }

    [Fact]
    public void ShowEmptyMessageWhenThereAreNoRates()
    {
        _rateService.IndexAnswer = Result.Success(new RateResponse.Index
        {
            Rates = [],
            RoomNames = [],
            Policies = [],
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
        cut.FindAll(".rate-create-fields input")[^1].Change("Flexible cancellation");

        cut.Find(".rate-create-panel form").Submit();

        cut.Find(".rate-filter .help").TextContent.ShouldContain("Select a room type.");
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
            request.Description.ShouldBe("Flexible cancellation");
        });
        cut.Find(".rate-table tbody").TextContent.ShouldContain("Standard");
        cut.Find(".rate-table tbody").TextContent.ShouldContain("Deluxe Room");
        cut.Find(".rate-table tbody").TextContent.ShouldContain("€79.00");
        cut.FindAll(".rate-create-panel").ShouldBeEmpty();
    }

    private static void OpenAndFillCreateForm(IRenderedComponent<Rates> cut)
    {
        cut.Find(".rate-page-heading button").Click();
        cut.FindAll(".rate-create-fields input")[0].Change("Standard");
        cut.Find("input[type='number']").Change("79");
        cut.FindAll(".rate-create-fields input")[^1].Change("Flexible cancellation");
        cut.Find(".rate-create-fields select").Change("Deluxe Room");
    }
}
