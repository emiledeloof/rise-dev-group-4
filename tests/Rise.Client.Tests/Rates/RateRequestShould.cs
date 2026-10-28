using Rise.Shared.Rates;
using Shouldly;

namespace Rise.Client.Rates;

public class RateRequestShould
{
    [Fact]
    public void IncludeFilterValuesInTheRatesQueryString()
    {
        var request = new RateRequest.Index
        {
            Skip = 10,
            Take = 10,
            RoomName = "Double Room",
            Policy = "No refund & more",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 31),
            FilterByDateRange = true,
        };

        var query = request.ToString();

        query.ShouldContain("&RoomName=Double%20Room");
        query.ShouldContain("&Policy=No%20refund%20%26%20more");
        query.ShouldContain("&Skip=10");
        query.ShouldContain("&Take=10");
        query.ShouldContain("&StartDate=2026-10-01");
        query.ShouldContain("&EndDate=2026-10-31");
        query.ShouldContain("&FilterByDateRange=True");
    }

    [Fact]
    public void LeaveEmptyFiltersUnselectedInTheQueryString()
    {
        var query = new RateRequest.Index().ToString();

        query.ShouldContain("&RoomName=");
        query.ShouldContain("&Policy=");
        query.ShouldNotContain("&StartDate=");
        query.ShouldNotContain("&EndDate=");
    }

    [Fact]
    public void IncludeOnlyTheSpecifiedDateForAnOpenEndedDateRange()
    {
        var query = new RateRequest.Index
        {
            EndDate = new DateOnly(2026, 10, 31),
            FilterByDateRange = true,
        }.ToString();

        query.ShouldNotContain("&StartDate=");
        query.ShouldContain("&EndDate=2026-10-31");
        query.ShouldContain("&FilterByDateRange=True");
    }
}
