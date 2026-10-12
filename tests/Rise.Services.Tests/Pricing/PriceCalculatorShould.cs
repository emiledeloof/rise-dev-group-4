using Rise.Services.Pricing;

namespace Rise.Services.Tests.Pricing;

/// <summary>
/// tests for <see cref="PriceCalculator"/>, plain c# so no db needed
/// </summary>
public class PriceCalculatorShould
{
    /// <summary>
    /// the total is the price of 1 night times the nb of nights
    /// Theory runs the same test once per InlineData line
    /// </summary>
    [Theory]
    [InlineData(160, 3, 480)]
    [InlineData(160, 1, 160)]
    [InlineData(99.50, 2, 199)]
    public void MultiplyThePriceByTheNights(decimal pricePerNight, int nights, decimal expected)
    {
        PriceCalculator.Total(pricePerNight, nights).ShouldBe(expected);
    }

    /// <summary>
    /// a stay without nights is a bug, the calculator refuses it instead of giving a wrong price
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RefuseAStayWithoutNights(int nights)
    {
        // Should.Throw checks that the code inside the lambda throws this exact exception
        Should.Throw<ArgumentOutOfRangeException>(() => PriceCalculator.Total(160m, nights));
    }
}
