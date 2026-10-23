using Rise.Shared.Rooms;
using Shouldly;

namespace Rise.Client.Components;

/// <summary>
/// tests for <see cref="CancellationPolicyText"/>, plain c# so no rendering needed
/// </summary>
public class CancellationPolicyTextShould
{
    // a rate with the client rules, each test changes what it checks
    private static RateDto.Available Rate(bool isRefundable) => new()
    {
        Id = 1,
        Name = "Flexible",
        PricePerNight = 109m,
        TotalPrice = 327m,
        IsRefundable = isRefundable,
        FullRefundDays = 30,
        PartialRefundDays = 7,
        PartialRefundPercent = 50,
    };

    /// <summary>
    /// a refundable rate shows its days and pct
    /// </summary>
    [Fact]
    public void ShowTheDaysAndPctOfARefundableRate()
    {
        CancellationPolicyText.Of(Rate(isRefundable: true))
            .ShouldBe("Free cancellation up to 30 days before arrival, 50% refund up to 7 days before");
    }

    /// <summary>
    /// the values come from the rate, not fixed in the text
    /// </summary>
    [Fact]
    public void UseTheValuesOfTheRate()
    {
        var rate = Rate(isRefundable: true);
        rate.FullRefundDays = 45;
        rate.PartialRefundDays = 10;
        rate.PartialRefundPercent = 30;

        CancellationPolicyText.Of(rate)
            .ShouldBe("Free cancellation up to 45 days before arrival, 30% refund up to 10 days before");
    }

    /// <summary>
    /// ! a non refundable rate ignores the days and pct
    /// </summary>
    [Fact]
    public void SayNonRefundableForANonRefundableRate()
    {
        CancellationPolicyText.Of(Rate(isRefundable: false)).ShouldBe("Non-refundable, no refund if you cancel");
    }
}
