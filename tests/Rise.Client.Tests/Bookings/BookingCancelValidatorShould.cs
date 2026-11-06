using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Client.Bookings;

/// <summary>
/// tests for <see cref="BookingRequest.Cancel.Validator"/>, the same rules run on the client and on the server
/// </summary>
public class BookingCancelValidatorShould
{
    private readonly BookingRequest.Cancel.Validator _validator = new();

    // a valid request, each test breaks one rule
    private static BookingRequest.Cancel ValidRequest() => new()
    {
        BookingId = 7,
        Reason = "Guest is ill",
        Rooms =
        [
            new() { BookingRoomId = 21, RefundAmount = 150m },
            new() { BookingRoomId = 23, RefundAmount = 0m },
        ],
    };

    [Fact]
    public void AcceptAValidRequest()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RejectAMissingReason(string? reason)
    {
        var request = ValidRequest();
        request.Reason = reason!;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(x => x.ErrorMessage == "Please enter a reason for the cancellation.");
    }

    [Fact]
    public void RejectAReasonThatIsTooLong()
    {
        var request = ValidRequest();
        request.Reason = new string('x', 501);

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AcceptAReasonOfTheMaximumLength()
    {
        var request = ValidRequest();
        request.Reason = new string('x', 500);

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectARequestWithoutRooms()
    {
        var request = ValidRequest();
        request.Rooms = [];

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(x => x.ErrorMessage == "Please select at least one room to cancel.");
    }

    [Fact]
    public void RejectTheSameRoomTwice()
    {
        var request = ValidRequest();
        request.Rooms[1].BookingRoomId = 21;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(x => x.ErrorMessage == "A room can only be cancelled once.");
    }

    [Fact]
    public void RejectANegativeRefundOfOneRoom()
    {
        var request = ValidRequest();
        request.Rooms[1].RefundAmount = -0.01m;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(x => x.ErrorMessage == "The refund can't be negative.");
    }

    [Fact]
    public void AcceptNoNewNbOfGuests()
    {
        var request = ValidRequest();
        request.Adults = null;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AcceptANewNbOfGuests()
    {
        var request = ValidRequest();
        request.Adults = 1;

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectANewNbOfGuestsBelowOne(int adults)
    {
        var request = ValidRequest();
        request.Adults = adults;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(x => x.ErrorMessage == "Please enter at least 1 guest.");
    }
}
