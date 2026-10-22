using Rise.Shared.Rates;
using Shouldly;

namespace Rise.Client.Rates;

public class RateCreateValidatorShould
{
    private readonly RateRequest.Create.Validator _validator = new();

    [Theory]
    [InlineData("", "Deluxe Room", 79, "Flexible cancellation", "Rate name is required.")]
    [InlineData("Standard", "", 79, "Flexible cancellation", "Select a room type.")]
    [InlineData("Standard", "Deluxe Room", 79, "", "Cancellation policy / description is required.")]
    [InlineData("Standard", "Deluxe Room", 0, "Flexible cancellation", "Price per night must be greater than 0.")]
    [InlineData("Standard", "Deluxe Room", 79.123, "Flexible cancellation", "Price per night can have at most 2 decimal places.")]
    public void ExplainInvalidCreateRateValues(
        string name,
        string roomName,
        decimal pricePerNight,
        string description,
        string expectedMessage)
    {
        var result = _validator.Validate(new RateRequest.Create
        {
            Name = name,
            RoomName = roomName,
            PricePerNight = pricePerNight,
            Description = description,
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorMessage).ShouldContain(expectedMessage);
    }

    [Fact]
    public void RejectRateNamesOverTheConfiguredLimit()
    {
        var result = _validator.Validate(new RateRequest.Create
        {
            Name = new string('a', 101),
            RoomName = "Deluxe Room",
            PricePerNight = 79,
            Description = "Flexible cancellation",
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .ShouldContain("Rate name cannot exceed 100 characters.");
    }

    [Fact]
    public void RejectDescriptionsOverTheConfiguredLimit()
    {
        var result = _validator.Validate(new RateRequest.Create
        {
            Name = "Standard",
            RoomName = "Deluxe Room",
            PricePerNight = 79,
            Description = new string('a', 501),
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorMessage)
            .ShouldContain("Cancellation policy / description cannot exceed 500 characters.");
    }
}
