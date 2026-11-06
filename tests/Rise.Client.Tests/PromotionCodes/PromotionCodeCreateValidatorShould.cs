using Rise.Shared.PromotionCodes;
using Shouldly;

namespace Rise.Client.Tests.PromotionCodes;

public class PromotionCodeCreateValidatorShould
{
    private readonly PromotionCodeRequest.Create.Validator _validator = new();

    private static PromotionCodeRequest.Create ValidRequest() => new()
    {
        Code = "SPRING2026",
        DiscountType = PromotionCodeDiscountType.Percentage,
        Value = 15m,
        ValidFrom = PromotionCodeRequest.GetCurrentDate(),
        ValidTo = PromotionCodeRequest.GetCurrentDate().AddDays(30),
        EmailRestriction = null,
        AppliesTo = PromotionCodeAppliesTo.AllRates,
        IsActive = true,
    };

    [Fact]
    public void AcceptAValidRequest()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseEmptyOrWhitespaceCode(string code)
    {
        var request = ValidRequest();
        request.Code = code;

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.Code));
    }

    [Theory]
    [InlineData("CODE WITH SPACE")]
    [InlineData("CODE!")]
    [InlineData("SAVE@50")]
    public void RefuseCodeWithInvalidCharacters(string code)
    {
        var request = ValidRequest();
        request.Code = code;

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.Code));
    }

    [Fact]
    public void RefuseCodeLongerThan50Chars()
    {
        var request = ValidRequest();
        request.Code = new string('A', 51);

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.Code));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void RefuseNegativeOrZeroValue(decimal value)
    {
        var request = ValidRequest();
        request.Value = value;

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.Value));
    }

    [Fact]
    public void RefuseValueGreaterThan100000()
    {
        var request = ValidRequest();
        request.DiscountType = PromotionCodeDiscountType.Fixed;
        request.Value = 100_001;

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.Value));
    }

    [Fact]
    public void RefusePercentageDiscountGreaterThan100()
    {
        var request = ValidRequest();
        request.DiscountType = PromotionCodeDiscountType.Percentage;
        request.Value = 100.5m;

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseStartDateInThePast()
    {
        var request = ValidRequest();
        request.ValidFrom = PromotionCodeRequest.GetCurrentDate().AddDays(-1);

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.ValidFrom));
    }

    [Fact]
    public void RefuseEndDateBeforeStartDate()
    {
        var request = ValidRequest();
        request.ValidFrom = PromotionCodeRequest.GetCurrentDate().AddDays(10);
        request.ValidTo = PromotionCodeRequest.GetCurrentDate().AddDays(5);

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.ValidTo));
    }

    [Fact]
    public void RefuseEmailLongerThan320Chars()
    {
        var request = ValidRequest();
        // A long string with an '@' and '.' that exceeds 320 chars
        request.EmailRestriction = new string('a', 315) + "@b.com";

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.EmailRestriction));
    }

    [Fact]
    public void RefuseInvalidEmailFormat()
    {
        var request = ValidRequest();
        request.EmailRestriction = "not-an-email";

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PromotionCodeRequest.Create.EmailRestriction));
    }

    [Fact]
    public void AllowEmptyEmailRestriction()
    {
        var request = ValidRequest();
        request.EmailRestriction = "";

        var result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }
}

