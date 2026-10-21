using Ardalis.Result;
using Rise.Shared.Identity;
using Rise.Shared.Rates;
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
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void ShowRateDetailsAndFilterOptions()
    {
        var cut = Render<Rates1>();

        var rows = cut.FindAll(".rate-table tbody tr");
        rows.Count.ShouldBe(2);
        rows[0].TextContent.ShouldContain("Non-refundable");
        rows[0].TextContent.ShouldContain("Double Room");
        rows[0].TextContent.ShouldContain("€90");

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
        var cut = Render<Rates1>();

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

        var cut = Render<Rates1>();

        cut.Markup.ShouldContain("No rates yet.");
        cut.FindAll(".rate-table tbody tr").ShouldBeEmpty();
    }

    [Fact]
    public void ShowAnErrorWhenRatesCouldNotBeLoaded()
    {
        _rateService.IndexAnswer = Result.Error("Service unavailable");

        var cut = Render<Rates1>();

        cut.Find(".notification.is-danger").TextContent.ShouldContain("The rates could not be loaded.");
    }
}
