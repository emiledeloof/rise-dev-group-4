using Shouldly;

namespace Rise.Client.Payments;

/// <summary>
/// tests for the page Stripe sends the guest to after cancelling, see Payments/Cancel.razor
/// </summary>
public class CancelShould : BunitContext
{
    [Fact]
    public void SayThePaymentWasNotCompleted()
    {
        // act
        var cut = Render<Cancel>();

        // assert
        cut.Find("h1").TextContent.ShouldBe("Payment cancelled");
        cut.Find(".cancel-message").TextContent.ShouldContain("not paid");
        cut.Find("a").TextContent.Trim().ShouldBe("Back to home");
    }
}
