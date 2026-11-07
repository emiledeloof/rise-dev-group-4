using Rise.Shared.Billing;
using Shouldly;

namespace Rise.Client.Billing;

/// <summary>
/// tests for the validator of an on-site payment, the same rules run on the client and on the server
/// </summary>
public class PaymentValidatorShould
{
    private readonly BillingRequest.RecordPayment.Validator _validator = new();

    // a valid payment, each test breaks one rule
    private static BillingRequest.RecordPayment ValidPayment() => new()
    {
        BookingId = 7,
        Amount = 16m,
        Method = PaymentMethods.Cash,
        Reference = "Receipt 0042",
    };

    [Fact]
    public void AcceptAValidPayment()
    {
        _validator.Validate(ValidPayment()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AcceptAPaymentWithoutReference()
    {
        var payment = ValidPayment();
        payment.Reference = null;

        _validator.Validate(payment).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100_000.01)]
    [InlineData(10.555)]
    public void RejectAnInvalidAmount(decimal amount)
    {
        var payment = ValidPayment();
        payment.Amount = amount;

        _validator.Validate(payment).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(PaymentMethods.CardTerminal)]
    [InlineData(PaymentMethods.BankTransfer)]
    public void AcceptEachOnSiteMethod(string method)
    {
        var payment = ValidPayment();
        payment.Method = method;

        _validator.Validate(payment).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PaymentMethods.Online)]
    [InlineData("")]
    [InlineData("Bitcoin")]
    public void RejectAMethodThatIsNotOnSite(string method)
    {
        var payment = ValidPayment();
        payment.Method = method;

        _validator.Validate(payment).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectATooLongReference()
    {
        var payment = ValidPayment();
        payment.Reference = new string('x', BillingRequest.RecordPayment.ReferenceMaxLength + 1);

        _validator.Validate(payment).IsValid.ShouldBeFalse();
    }
}
