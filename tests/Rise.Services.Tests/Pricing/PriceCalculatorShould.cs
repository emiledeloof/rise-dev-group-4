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

    [Theory]
    [InlineData(109, true, 20, 130.80)]
    [InlineData(109, true, 0, 109)]
    [InlineData(109, false, 20, 109)]
    [InlineData(10.01, true, 5, 10.51)]
    public void ApplyVatOnlyWhenEnabledAndRoundToCurrencyPrecision(
        decimal price,
        bool isVatEnabled,
        decimal vatPercentage,
        decimal expected)
    {
        PriceCalculator.InclusiveOfVat(price, isVatEnabled, vatPercentage).ShouldBe(expected);
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

    /// <summary>
    /// the VAT inside a price that already holds it, eg 122 at 22 % holds 22
    /// </summary>
    [Theory]
    [InlineData(122, 22, 22)]
    [InlineData(110, 10, 10)]
    [InlineData(100, 0, 0)]
    [InlineData(10, 22, 1.80)]
    public void TakeTheVatOutOfAPriceIncludingVat(decimal gross, decimal vatPercentage, decimal expected)
    {
        PriceCalculator.VatIncluded(gross, vatPercentage).ShouldBe(expected);
    }
}
