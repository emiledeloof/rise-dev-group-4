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
}
