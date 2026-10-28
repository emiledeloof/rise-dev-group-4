using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Client.Bookings;

/// <summary>
/// tests for <see cref="BookingRequest.Create.Validator"/>, the same rules run on the client and on the server
/// </summary>
public class BookingCreateValidatorShould
{
    private readonly BookingRequest.Create.Validator _validator = new();

    // a valid request, each test breaks one rule
    private static BookingRequest.Create ValidRequest() => TestDrafts.WithRooms(withGuest: true).ToCreateRequest();

    [Fact]
    public void AcceptAValidRequest()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectACheckInInThePast()
    {
        var request = ValidRequest();
        request.CheckIn = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectACheckOutBeforeTheCheckIn()
    {
        var request = ValidRequest();
        request.CheckOut = request.CheckIn;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void TakeTheNbOfGuestsOfTheDraft()
    {
        ValidRequest().Adults.ShouldBe(TestDrafts.Adults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectABookingWithoutGuests(int adults)
    {
        var request = ValidRequest();
        request.Adults = adults;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectABookingWithoutRooms()
    {
        var request = ValidRequest();
        request.Rooms.Clear();

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectTheSameRoomTwice()
    {
        var request = ValidRequest();
        request.Rooms.Add(new BookingRequest.Create.RoomSelection { RoomId = 1, RateId = 2 });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectInvalidGuestDetails()
    {
        var request = ValidRequest();
        request.Guest.Email = "not-an-email";

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }
}
