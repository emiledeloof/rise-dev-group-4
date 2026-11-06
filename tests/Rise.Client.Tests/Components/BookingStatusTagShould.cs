using Rise.Shared.Bookings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for <see cref="BookingStatusTag"/>, the color tells the admin at a glance if a booking is paid
/// </summary>
public class BookingStatusTagShould : BunitContext
{
    public BookingStatusTagShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    [Theory]
    [InlineData(BookingStatus.Paid, "is-primary", "Paid")]
    [InlineData(BookingStatus.Cancelled, "is-danger", "Cancelled")]
    [InlineData(BookingStatus.Pending, "is-warning", "Pending")]
    public void ShowTheStatusInItsColor(string status, string expectedClass, string expectedText)
    {
        var cut = Render<BookingStatusTag>(p => p.Add(x => x.Status, status));

        var tag = cut.Find(".tag");
        tag.ClassList.ShouldContain(expectedClass);
        tag.TextContent.Trim().ShouldBe(expectedText);
    }

    [Fact]
    public void ShowAnUnknownStatusAsPending()
    {
        var cut = Render<BookingStatusTag>(p => p.Add(x => x.Status, "Refunded"));

        cut.Find(".tag").TextContent.Trim().ShouldBe("Pending");
    }
}
