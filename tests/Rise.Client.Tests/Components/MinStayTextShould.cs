using Rise.Shared.Rooms;
using Shouldly;

namespace Rise.Client.Components;

/// <summary>
/// tests for <see cref="MinStayText"/>, plain c# so no rendering needed
/// </summary>
public class MinStayTextShould
{
    // a rate with only the min nb of nights changing
    private static RateDto.Available Rate(int minNights) => new()
    {
        Id = 1,
        Name = "Non-refundable",
        PricePerNight = 109m,
        TotalPrice = 327m,
        IsRefundable = false,
        FullRefundDays = 30,
        PartialRefundDays = 7,
        PartialRefundPercent = 50,
        MinNights = minNights,
    };

    /// <summary>
    /// a rate without minimum gets no line
    /// </summary>
    [Fact]
    public void GiveNoTextForARateWithoutMinimum()
    {
        MinStayText.Of(Rate(minNights: 1), nights: 3).ShouldBeNull();
    }

    /// <summary>
    /// a minimum the stay meets is shown as a condition
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 3)] // exactly the min nb of nights is allowed
    public void ShowTheMinimumWhenTheStayIsLongEnough(int minNights, int nights)
    {
        MinStayText.Of(Rate(minNights), nights).ShouldBe($"Minimum stay {minNights} nights");
        MinStayText.IsBookable(Rate(minNights), nights).ShouldBeTrue();
    }

    /// <summary>
    /// a stay shorter than the minimum says what is needed
    /// </summary>
    [Fact]
    public void SayALongerStayIsNeededWhenTheStayIsTooShort()
    {
        MinStayText.Of(Rate(minNights: 5), nights: 3).ShouldBe("Requires a stay of at least 5 nights");
        MinStayText.IsBookable(Rate(minNights: 5), nights: 3).ShouldBeFalse();
    }
}
