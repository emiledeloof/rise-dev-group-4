using Rise.Shared.Billing;
using Shouldly;

namespace Rise.Client.Billing;

/// <summary>
/// tests for the validators of a free bill line, the same rules run on the client and on the server
/// </summary>
public class BillLineValidatorShould
{
    private readonly BillingRequest.EditLine.Validator _validator = new();

    // a valid line, each test breaks one rule
    private static BillingRequest.EditLine ValidLine() => new()
    {
        BookingId = 7,
        LineId = 11,
        Description = "Minibar",
        Quantity = 2,
        UnitPrice = 8m,
        VatPercentage = 22m,
    };

    [Fact]
    public void AcceptAValidLine()
    {
        _validator.Validate(ValidLine()).IsValid.ShouldBeTrue();
        new BillingRequest.AddLine.Validator().Validate(ValidLine()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectAMissingDescription(string description)
    {
        var line = ValidLine();
        line.Description = description;

        _validator.Validate(line).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectATooLongDescription()
    {
        var line = ValidLine();
        line.Description = new string('a', 201);

        _validator.Validate(line).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void RejectAQuantityOutOfRange(int quantity)
    {
        var line = ValidLine();
        line.Quantity = quantity;

        _validator.Validate(line).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000.01)]
    [InlineData(1.005)]
    public void RejectAnInvalidPrice(decimal unitPrice)
    {
        var line = ValidLine();
        line.UnitPrice = unitPrice;

        _validator.Validate(line).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(22)]
    public void AcceptTheItalianVatRates(decimal vatPercentage)
    {
        var line = ValidLine();
        line.VatPercentage = vatPercentage;

        _validator.Validate(line).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(6)]
    [InlineData(21)]
    public void RejectAVatPercentageThatIsNoPreset(decimal vatPercentage)
    {
        var line = ValidLine();
        line.VatPercentage = vatPercentage;

        _validator.Validate(line).IsValid.ShouldBeFalse();
    }
}
