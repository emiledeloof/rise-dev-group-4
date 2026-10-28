using Rise.Shared.RatePricing;
using Shouldly;

namespace Rise.Client.RatePricing;

/// <summary>
/// tests for <see cref="RatePricingRequest.Edit.Validator"/>, same rules on the client and on the server
/// same idea as Bookings/BookingCreateValidatorShould.cs
/// </summary>
public class RatePricingEditValidatorShould
{
    private readonly RatePricingRequest.Edit.Validator _validator = new();

    // a valid request, each test breaks one rule
    private static RatePricingRequest.Edit ValidRequest() => new() { RateId = 1, PricePerNight = 175m, MinNights = 1 };

    /// <summary>
    /// a normal price passes
    /// </summary>
    [Fact]
    public void AcceptAValidPrice()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectAnEndDateBeforeTheStartDate()
    {
        var request = ValidRequest();
        request.StartDate = new DateOnly(2026, 10, 31);
        request.EndDate = new DateOnly(2026, 10, 1);

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .ShouldContain("Start date must be before end date.");
    }

    /// <summary>
    /// the lowest price that still passes, 1 cent
    /// </summary>
    [Fact]
    public void AcceptOneCent()
    {
        var request = ValidRequest();
        request.PricePerNight = 0.01m;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// 0 and negative prices are refused, Theory runs the test once per InlineData line
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RejectAPriceOfZeroOrLess(decimal price)
    {
        var request = ValidRequest();
        request.PricePerNight = price;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// the db keeps 2 decimals, a 3rd one would be lost
    /// </summary>
    [Fact]
    public void RejectMoreThanTwoDecimals()
    {
        var request = ValidRequest();
        request.PricePerNight = 175.123m;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// no rate chosen, id 0 is never a real rate
    /// </summary>
    [Fact]
    public void RejectAMissingRate()
    {
        var request = ValidRequest();
        request.RateId = 0;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// a stay is at least 1 night, 0 and negative are refused
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectMinNightsOfZeroOrLess(int minNights)
    {
        var request = ValidRequest();
        request.MinNights = minNights;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    // a valid refundable request, the default values of the client
    private static RatePricingRequest.Edit ValidRefundableRequest() => new()
    {
        RateId = 1,
        PricePerNight = 175m,
        MinNights = 1,
        IsRefundable = true,
        FullRefundDays = 30,
        PartialRefundDays = 7,
        PartialRefundPercent = 50
    };

    /// <summary>
    /// 100% up to 30 days, 50% up to 7 days passes
    /// </summary>
    [Fact]
    public void AcceptValidRefundRules()
    {
        _validator.Validate(ValidRefundableRequest()).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// 0 and 100 are the limits, both still pass
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void AcceptAPartialRefundOfZeroOrHundred(int percent)
    {
        var request = ValidRefundableRequest();
        request.PartialRefundPercent = percent;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// a pct outside 0 to 100 makes no sense
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RejectAPartialRefundOutsideZeroToHundred(int percent)
    {
        var request = ValidRefundableRequest();
        request.PartialRefundPercent = percent;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// the full refund period must be strictly longer than the partial one
    /// </summary>
    [Theory]
    [InlineData(7, 7)]
    [InlineData(5, 7)]
    public void RejectAFullPeriodNotLongerThanThePartialOne(int fullDays, int partialDays)
    {
        var request = ValidRefundableRequest();
        request.FullRefundDays = fullDays;
        request.PartialRefundDays = partialDays;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// a negative nb of days is refused
    /// </summary>
    [Fact]
    public void RejectNegativePartialDays()
    {
        var request = ValidRefundableRequest();
        request.PartialRefundDays = -1;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// ! for a non refundable rate the refund values are not checked, the .When skips them
    /// </summary>
    [Fact]
    public void IgnoreRefundValuesForANonRefundableRate()
    {
        var request = ValidRequest();
        request.IsRefundable = false;
        request.FullRefundDays = 0;
        request.PartialRefundDays = -1;
        request.PartialRefundPercent = 150;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }
}
