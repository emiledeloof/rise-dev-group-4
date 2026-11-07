using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Client.Bookings;

/// <summary>
/// tests for <see cref="BookingRequest.RecordRefund.Validator"/>, the same rules run on the client and on the server
/// </summary>
public class BookingRefundValidatorShould
{
    private readonly BookingRequest.RecordRefund.Validator _validator = new();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // a valid request, each test breaks one rule
    private static BookingRequest.RecordRefund ValidRequest() => new()
    {
        BookingId = 7,
        BookingRoomId = 23,
        PaidOn = Today,
        Amount = 210m,
        Note = "Bank transfer",
    };

    [Fact]
    public void AcceptAValidRequest()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AcceptARequestWithoutNote()
    {
        var request = ValidRequest();
        request.Note = null;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RejectAnAmountOfZeroOrLess(decimal amount)
    {
        var request = ValidRequest();
        request.Amount = amount;

        var result = _validator.Validate(request);

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Please enter the amount that was paid back.");
    }

    [Fact]
    public void RejectADateInTheFuture()
    {
        var request = ValidRequest();
        request.PaidOn = Today.AddDays(2);

        var result = _validator.Validate(request);

        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("The payment date can't be in the future.");
    }

    [Fact]
    public void AcceptAPastDate()
    {
        var request = ValidRequest();
        request.PaidOn = Today.AddDays(-10);

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectATooLongNote()
    {
        var request = ValidRequest();
        request.Note = new string('x', BookingRequest.RecordRefund.Validator.NoteMaxLength + 1);

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }
}
