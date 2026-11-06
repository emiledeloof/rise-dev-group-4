using Shouldly;

namespace Rise.Client.Common;

/// <summary>
/// tests for <see cref="Format"/>, plain c# so no rendering needed
/// </summary>
public class FormatShould
{
    [Theory]
    [InlineData(432, "€432.00")]
    [InlineData(99.5, "€99.50")]
    [InlineData(0, "€0.00")]
    public void WriteAnAmountWithTwoDecimals(decimal amount, string expected)
    {
        Format.Amount(amount).ShouldBe(expected);
    }

    [Theory]
    [InlineData(21, "21 %")]
    [InlineData(5.5, "5.5 %")]
    [InlineData(10.00, "10 %")]
    public void WriteAPercentWithoutTrailingZeros(decimal percentage, string expected)
    {
        Format.Percent(percentage).ShouldBe(expected);
    }

    [Fact]
    public void WriteADateTheSameWayForDateOnlyAndDateTime()
    {
        Format.Date(new DateOnly(2026, 10, 2)).ShouldBe("02 Oct 2026");
        // * the time is left out
        Format.Date(new DateTime(2026, 10, 2, 18, 30, 0)).ShouldBe("02 Oct 2026");
    }

    [Theory]
    [InlineData("visa", "Visa •••• 4242")]
    [InlineData("mastercard", "Mastercard •••• 4242")]
    [InlineData(null, "Card •••• 4242")]
    [InlineData("  ", "Card •••• 4242")]
    public void WriteTheCardBrandWithACapital(string? brand, string expected)
    {
        Format.Card(brand, "4242").ShouldBe(expected);
    }
}
