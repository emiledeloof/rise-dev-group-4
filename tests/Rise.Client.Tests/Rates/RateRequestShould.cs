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
        };

        var query = request.ToString();

        query.ShouldContain("&RoomName=Double%20Room");
        query.ShouldContain("&Policy=No%20refund%20%26%20more");
        query.ShouldContain("&Skip=10");
        query.ShouldContain("&Take=10");
    }

    [Fact]
    public void LeaveEmptyFiltersUnselectedInTheQueryString()
    {
        var query = new RateRequest.Index().ToString();

        query.ShouldContain("&RoomName=");
        query.ShouldContain("&Policy=");
    }
}
