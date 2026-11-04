using Rise.Shared.RoomTypes;
using Shouldly;

namespace Rise.Client.RoomTypes;

public class RoomTypeValidatorShould
{
    [Fact]
    public void AcceptAValidCreateRequest()
    {
        var result = new RoomTypeRequest.Create.Validator().Validate(new RoomTypeRequest.Create { Name = "Deluxe Room" });

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseAnEmptyName(string name)
    {
        var result = new RoomTypeRequest.Create.Validator().Validate(new RoomTypeRequest.Create { Name = name });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseATooLongName()
    {
        var result = new RoomTypeRequest.Create.Validator().Validate(new RoomTypeRequest.Create { Name = new string('a', 101) });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseAnEditWithoutId()
    {
        var result = new RoomTypeRequest.Edit.Validator().Validate(new RoomTypeRequest.Edit { Name = "Deluxe Room" });

        result.IsValid.ShouldBeFalse();
    }
}
