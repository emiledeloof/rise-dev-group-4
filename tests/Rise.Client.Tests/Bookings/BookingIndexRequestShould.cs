using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Client.Bookings;

public class BookingIndexRequestShould
{
    [Fact]
    public void IncludeTheStatusFilterInTheQueryString()
    {
        var query = new BookingRequest.Index
        {
            Skip = 10,
            Take = 10,
            Status = BookingStatusFilters.Cancelled,
        }.ToString();

        query.ShouldContain("&Skip=10");
        query.ShouldContain("&Take=10");
        query.ShouldContain("&Status=Cancelled");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(BookingStatusFilters.All)]
    [InlineData(BookingStatusFilters.NotCancelled)]
    [InlineData(BookingStatusFilters.Pending)]
    [InlineData(BookingStatusFilters.Paid)]
    [InlineData(BookingStatusFilters.PartiallyCancelled)]
    [InlineData(BookingStatusFilters.Cancelled)]
    [InlineData(BookingStatusFilters.RefundPending)]
    [InlineData(BookingStatusFilters.Refunded)]
    public void AcceptAKnownOrEmptyFilter(string? filter)
    {
        var result = new BookingRequest.Index.Validator().Validate(new BookingRequest.Index { Status = filter });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Refund")]
    [InlineData("cancelled")]
    public void RejectAnUnknownFilter(string filter)
    {
        var result = new BookingRequest.Index.Validator().Validate(new BookingRequest.Index { Status = filter });

        result.IsValid.ShouldBeFalse();
    }
}
