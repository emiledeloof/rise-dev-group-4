using Rise.Shared.Bookings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for <see cref="RefundStatusTag"/>
/// </summary>
public class RefundStatusTagShould : BunitContext
{
    public RefundStatusTagShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    [Theory]
    [InlineData(BookingRefundStatus.RefundPending, "refund-tag-pending", "Refund pending")]
    [InlineData(BookingRefundStatus.Refunded, "refund-tag-refunded", "Refunded")]
    [InlineData(BookingRefundStatus.NoRefund, "refund-tag-none", "No refund")]
    public void ShowTheRefundStatus(string status, string cssClass, string text)
    {
        var cut = Render<RefundStatusTag>(p => p.Add(x => x.Status, status));

        var tag = cut.Find(".refund-tag");
        tag.ClassList.ShouldContain(cssClass);
        tag.TextContent.Trim().ShouldBe(text);
    }

    [Fact]
    public void ShowNothingWhenNoRoomIsCancelled()
    {
        var cut = Render<RefundStatusTag>(p => p.Add(x => x.Status, BookingRefundStatus.None));

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void BeMediumInTheHeader()
    {
        var cut = Render<RefundStatusTag>(p => p
            .Add(x => x.Status, BookingRefundStatus.RefundPending)
            .Add(x => x.IsMedium, true));

        cut.Find(".refund-tag").ClassList.ShouldContain("is-medium");
    }
}
