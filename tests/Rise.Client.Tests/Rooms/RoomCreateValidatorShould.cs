using Rise.Shared.Rooms;
using Shouldly;

namespace Rise.Client.Rooms;

public class RoomCreateValidatorShould
{
    private readonly RoomRequest.Create.Validator _validator = new();

    private static RoomRequest.Create ValidRequest() => new()
    {
        RoomNumber = "104",
        RoomTypeId = 2,
        MaxGuests = 2,
    };

    [Fact]
    public void AcceptAValidRequest()
    {
        _validator.Validate(ValidRequest()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseAnEmptyRoomNumber(string roomNumber)
    {
        var request = ValidRequest();
        request.RoomNumber = roomNumber;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(e => e.ErrorMessage == "Room number is required.");
    }

    [Fact]
    public void RefuseATooLongRoomNumber()
    {
        var request = ValidRequest();
        request.RoomNumber = new string('1', 21);

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseAMissingRoomType()
    {
        var request = ValidRequest();
        request.RoomTypeId = 0;

        var result = _validator.Validate(request);

        result.Errors.ShouldContain(e => e.ErrorMessage == "Select a room type.");
    }

    [Fact]
    public void RefuseZeroGuests()
    {
        var request = ValidRequest();
        request.MaxGuests = 0;

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }
}
