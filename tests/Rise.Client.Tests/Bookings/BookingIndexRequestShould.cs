using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Client.Bookings;

public class BookingIndexRequestShould
{
    [Fact]
    public void IncludeTheCancellationFilterInTheQueryString()
    {
        var query = new BookingRequest.Index
        {
            Skip = 10,
            Take = 10,
            CancellationFilter = BookingCancellationFilters.OnlyCancelled,
        }.ToString();

        query.ShouldContain("&Skip=10");
        query.ShouldContain("&Take=10");
        query.ShouldContain("&CancellationFilter=OnlyCancelled");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(BookingCancellationFilters.All)]
    [InlineData(BookingCancellationFilters.NotCancelled)]
    [InlineData(BookingCancellationFilters.OnlyCancelled)]
    public void AcceptAKnownOrEmptyFilter(string? filter)
    {
        var result = new BookingRequest.Index.Validator().Validate(new BookingRequest.Index { CancellationFilter = filter });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Refunded")]
    [InlineData("onlycancelled")]
    public void RejectAnUnknownFilter(string filter)
    {
        var result = new BookingRequest.Index.Validator().Validate(new BookingRequest.Index { CancellationFilter = filter });

        result.IsValid.ShouldBeFalse();
    }
}
