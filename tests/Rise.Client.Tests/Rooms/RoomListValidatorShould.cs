using Rise.Shared.Rooms;
using Shouldly;

namespace Rise.Client.Rooms;

/// <summary>
/// tests for the take rule of the room list request
/// plain c# tests, no rendering needed because the validator is not a component
/// </summary>
public class RoomListValidatorShould
{
    // one validator reused by every test, xunit creates a new instance of the class per test anyway
    private readonly RoomRequest.List.Validator _validator = new();

    /// <summary>
    /// 1 to 20 rooms is fine, 3 is what the landing page asks for
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(20)]
    public void AcceptATakeBetweenOneAndTwenty(int take)
    {
        _validator.Validate(new RoomRequest.List { Take = take }).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// no room or too many rooms is refused
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void RejectATakeOutsideOneToTwenty(int take)
    {
        _validator.Validate(new RoomRequest.List { Take = take }).IsValid.ShouldBeFalse();
    }
}
