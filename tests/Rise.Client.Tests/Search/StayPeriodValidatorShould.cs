using Rise.Shared.Search;
using Shouldly;

namespace Rise.Client.Search;

/// <summary>
/// tests for the stay period rules used by the search form
/// plain c# tests, no rendering needed because the validator is not a component
/// </summary>
public class StayPeriodValidatorShould
{
    // dates are relative to today so the tests keep working whatever day they run
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    // one validator reused by every test, xunit creates a new instance of the class per test anyway
    private readonly SearchRequest.StayPeriod.Validator _validator = new();

    // shortcut to build a period relative to today, null means no date picked
    // eg Period(10, 13) is check in in 10 days and check out in 13 days, so 3 nights
    private static SearchRequest.StayPeriod Period(int? checkInInDays, int? checkOutInDays) => new()
    {
        CheckIn = checkInInDays is { } i ? Today.AddDays(i) : null,
        CheckOut = checkOutInDays is { } o ? Today.AddDays(o) : null,
    };

    /// <summary>
    /// a future stay of a few nights is fine
    /// </summary>
    [Fact]
    public void AcceptAValidPeriod()
    {
        // Validate runs every rule and returns a result, IsValid is true when no rule failed
        _validator.Validate(Period(10, 13)).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// arriving today is allowed
    /// </summary>
    [Fact]
    public void AcceptCheckInToday()
    {
        // 0 means today, the rule is check in greater or equal to today
        _validator.Validate(Period(0, 1)).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// both dates are required
    /// Theory runs the same test once per InlineData line, here 3 runs
    /// </summary>
    [Theory]
    [InlineData(null, 5)]
    [InlineData(5, null)]
    [InlineData(null, null)]
    public void RejectMissingDates(int? checkIn, int? checkOut)
    {
        // any missing date makes the whole period invalid
        _validator.Validate(Period(checkIn, checkOut)).IsValid.ShouldBeFalse();
    }

    /// <summary>
    /// past check in dates are refused
    /// </summary>
    [Fact]
    public void RejectCheckInInThePast()
    {
        // arrange and act, check in yesterday
        var result = _validator.Validate(Period(-1, 2));

        // assert, invalid and the error is on CheckIn, not on CheckOut
        // Errors is the list of failed rules, each one knows which property it is about
        // nameof gives the property name as text, safer than writing "CheckIn" by hand
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.CheckIn));
    }

    /// <summary>
    /// check out before or on the check in day is refused
    /// </summary>
    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 3)]
    public void RejectCheckOutNotAfterCheckIn(int checkIn, int checkOut)
    {
        // arrange and act, same day then check out before check in
        var result = _validator.Validate(Period(checkIn, checkOut));

        // assert, the error is on CheckOut
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.CheckOut));
    }

    /// <summary>
    /// nights are the days between both dates
    /// </summary>
    [Theory]
    [InlineData(5, 6, 1)]
    [InlineData(5, 8, 3)]
    public void CountNights(int checkIn, int checkOut, int expectedNights)
    {
        // Nights is a computed property, no validator needed
        Period(checkIn, checkOut).Nights.ShouldBe(expectedNights);
    }

    /// <summary>
    /// no nights when the period is incomplete or invalid
    /// </summary>
    [Theory]
    [InlineData(null, 5)]
    [InlineData(5, null)]
    [InlineData(5, 5)]
    [InlineData(5, 3)]
    public void CountZeroNightsForAnInvalidPeriod(int? checkIn, int? checkOut)
    {
        // missing date, same day or check out before check in all give 0, never a negative nb
        Period(checkIn, checkOut).Nights.ShouldBe(0);
    }

    /// <summary>
    /// guest counts between 1 and the maximum capacity with enough rooms are accepted
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(6, 3)]
    public void AcceptValidAdultsCount(int adults, int rooms)
    {
        var period = Period(10, 13);
        period.Adults = adults;
        period.Rooms = rooms;

        _validator.Validate(period).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// at least 1 adult is required (ENOG acceptance criteria)
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectLessThanOneAdult(int adults)
    {
        var period = Period(10, 13);
        period.Adults = adults;

        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Adults));
    }

    /// <summary>
    /// exceeding maximum capacity is refused when MaxCapacity is set
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(10)]
    public void RejectExceedingMaxCapacity(int adults)
    {
        var period = Period(10, 13);
        period.MaxCapacity = 6;
        period.Adults = adults;
        period.Rooms = 3;

        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Adults));
    }

    /// <summary>
    /// room counts with enough adults are accepted (ENOR)
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 4)]
    [InlineData(2, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 3)]
    [InlineData(3, 6)]
    public void AcceptValidRoomsCount(int rooms, int adults)
    {
        var period = Period(10, 13);
        period.Rooms = rooms;
        period.Adults = adults;

        _validator.Validate(period).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// at least 1 room is required (ENOR acceptance criteria)
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectLessThanOneRoom(int rooms)
    {
        var period = Period(10, 13);
        period.Rooms = rooms;

        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Rooms));
    }

    /// <summary>
    /// exceeding maximum rooms is refused when MaxRooms is set
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void RejectExceedingMaxRooms(int rooms)
    {
        var period = Period(10, 13);
        period.MaxRooms = 3;
        period.Rooms = rooms;
        period.Adults = rooms;

        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Rooms));
    }

    /// <summary>
    /// each room requires at least 1 adult (ENOR acceptance criteria)
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void RejectFewerAdultsThanRooms(int adults, int rooms)
    {
        var period = Period(10, 13);
        period.Adults = adults;
        period.Rooms = rooms;

        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Adults));
    }

    /// <summary>
    /// estate rooms have individual capacities in the database,
    /// so StayPeriod does not enforce an artificial 2 guests/room limit
    /// </summary>
    [Theory]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    public void AllowMoreThanTwoGuestsPerRoomWithoutStaticCap(int adults, int rooms)
    {
        var period = Period(10, 13);
        period.Adults = adults;
        period.Rooms = rooms;

        _validator.Validate(period).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// when MaxCapacity and MaxRooms are not set, higher counts are accepted
    /// </summary>
    [Fact]
    public void AcceptHigherCountsWhenMaxPropertiesNotSet()
    {
        var period = Period(10, 13);
        period.Rooms = 5;
        period.Adults = 10;
        period.MaxRooms = null;
        period.MaxCapacity = null;

        _validator.Validate(period).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// maximum capacity from room data is respected when set
    /// </summary>
    [Fact]
    public void RespectMaxCapacityWhenProvided()
    {
        var period = Period(10, 13);
        period.Rooms = 2;
        period.MaxCapacity = 4;
        period.Adults = 4;

        _validator.Validate(period).IsValid.ShouldBeTrue();

        // 5 adults exceeds maximum capacity of 4
        period.Adults = 5;
        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Adults));
    }

    /// <summary>
    /// maximum rooms from room data is respected when set
    /// </summary>
    [Fact]
    public void RespectMaxRoomsWhenProvided()
    {
        var period = Period(10, 13);
        period.MaxRooms = 2;
        period.Adults = 2;
        period.Rooms = 2;

        _validator.Validate(period).IsValid.ShouldBeTrue();

        // 3 rooms exceeds maximum rooms of 2
        period.Rooms = 3;
        period.Adults = 3;
        var result = _validator.Validate(period);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SearchRequest.StayPeriod.Rooms));
    }
}
