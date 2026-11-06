using Rise.Domain.Promotions;
using Shouldly;

namespace Rise.Domain.Tests.Promotions;

public class PromotionCodeShould
{
    private static PromotionCode CreateValidPromotionCode()
    {
        return new PromotionCode(
            "SUMMER2026",
            PromotionDiscountType.Percentage,
            20m,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            "guest@example.com",
            PromotionAppliesTo.AllRates);
    }

    [Fact]
    public void BeCreatedWithValidParameters()
    {
        var promo = CreateValidPromotionCode();

        promo.Code.ShouldBe("SUMMER2026");
        promo.DiscountType.ShouldBe(PromotionDiscountType.Percentage);
        promo.Value.ShouldBe(20m);
        promo.ValidFrom.ShouldBe(new DateOnly(2026, 6, 1));
        promo.ValidTo.ShouldBe(new DateOnly(2026, 8, 31));
        promo.EmailRestriction.ShouldBe("guest@example.com");
        promo.AppliesTo.ShouldBe(PromotionAppliesTo.AllRates);
        promo.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseNullOrWhitespaceCode(string? code)
    {
        Action act = () => new PromotionCode(
            code!,
            PromotionDiscountType.Percentage,
            10m,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            null,
            PromotionAppliesTo.AllRates);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData("PROMO CODE")]
    [InlineData("CODE!")]
    [InlineData("SAVE@50")]
    public void RefuseInvalidCodeFormat(string code)
    {
        Action act = () => new PromotionCode(
            code,
            PromotionDiscountType.Percentage,
            10m,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            null,
            PromotionAppliesTo.AllRates);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void RefuseCodeLongerThan50Chars()
    {
        var longCode = new string('A', 51);

        Action act = () => new PromotionCode(
            longCode,
            PromotionDiscountType.Percentage,
            10m,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            null,
            PromotionAppliesTo.AllRates);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(100.01)]
    [InlineData(150)]
    public void RefusePercentageDiscountExceeding100(decimal value)
    {
        Action act = () => new PromotionCode(
            "DISCOUNT",
            PromotionDiscountType.Percentage,
            value,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            null,
            PromotionAppliesTo.AllRates);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AllowFixedDiscountExceeding100()
    {
        var promo = new PromotionCode(
            "DISCOUNT",
            PromotionDiscountType.Fixed,
            250m,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 8, 31),
            null,
            PromotionAppliesTo.AllRates);

        promo.Value.ShouldBe(250m);
    }

    [Fact]
    public void RefuseValidToBeforeValidFrom()
    {
        Action act = () => new PromotionCode(
            "DISCOUNT",
            PromotionDiscountType.Percentage,
            10m,
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 6, 1),
            null,
            PromotionAppliesTo.AllRates);

        act.ShouldThrow<ArgumentException>();
    }
}

