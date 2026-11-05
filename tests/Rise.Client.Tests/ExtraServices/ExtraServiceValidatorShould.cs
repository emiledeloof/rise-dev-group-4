using Rise.Shared.ExtraServices;
using Shouldly;
using Xunit;

namespace Rise.Client.Tests.ExtraServices;

public class ExtraServiceValidatorShould
{
    [Fact]
    public void AcceptAValidCreateRequest()
    {
        var request = new ExtraServiceRequest.Create
        {
            Name = "Breakfast",
            Price = 15m,
            Cadence = ChargeCadence.PerNight,
            IsPerPerson = true,
            Description = "Delicious morning buffet"
        };

        var result = new ExtraServiceRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseAnEmptyName(string name)
    {
        var request = new ExtraServiceRequest.Create
        {
            Name = name,
            Price = 10m
        };

        var result = new ExtraServiceRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseANegativePrice()
    {
        var request = new ExtraServiceRequest.Create
        {
            Name = "Cleaning",
            Price = -5m
        };

        var result = new ExtraServiceRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AcceptZeroPrice()
    {
        var request = new ExtraServiceRequest.Create
        {
            Name = "Complimentary Welcome Drink",
            Price = 0m
        };

        var result = new ExtraServiceRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RefuseAnEditWithoutId()
    {
        var request = new ExtraServiceRequest.Edit
        {
            Id = 0,
            Name = "Valid Name",
            Price = 10m
        };

        var result = new ExtraServiceRequest.Edit.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }
}
