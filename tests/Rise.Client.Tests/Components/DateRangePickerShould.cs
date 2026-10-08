using System.Globalization;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="DateRangePicker"/> component
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// cut means component under test, it is the rendered calendar we inspect like a mini html page
/// </summary>
public class DateRangePickerShould : BunitContext
{
    // dates are relative to today so the tests keep working whatever day they run
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public DateRangePickerShould(ITestOutputHelper outputHelper)
    {
        // sends the app logs to the test output, handy when a test fails
        Services.AddXunitLogger(outputHelper);
    }

    // finds a day btn by its aria label, eg 12 october 2026
    // same format as in DateRangePicker.razor, invariant culture so it is always in english
    private static string DaySelector(DateOnly day) =>
        $"[aria-label='{day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}']";

    /// <summary>
    /// closed field invites the guest to pick dates
    /// </summary>
    [Fact]
    public void ShowAddDatesWhenEmpty()
    {
        // arrange and act, render the calendar without any date
        var cut = Render<DateRangePicker>();

        // assert, the field shows the placeholder text
        cut.Find(".drp-value").TextContent.ShouldBe("Add dates");

        // and the panel does not exist yet because it is closed
        cut.FindAll(".drp-panel").ShouldBeEmpty();
    }

    /// <summary>
    /// clicking the field opens the calendar
    /// </summary>
    [Fact]
    public void OpenCalendarOnClick()
    {
        // arrange
        var cut = Render<DateRangePicker>();

        // act, Click simulates a user click on the field
        cut.Find(".drp-field").Click();

        // assert, the panel is now in the html
        cut.FindAll(".drp-panel").Count.ShouldBe(1);
    }

    /// <summary>
    /// every day before today is disabled in the current month
    /// </summary>
    [Fact]
    public void DisablePastDays()
    {
        // arrange
        var cut = Render<DateRangePicker>();

        // act, open the calendar so the days are rendered
        cut.Find(".drp-field").Click();

        // assert, [disabled] in the selector keeps only disabled days
        // eg on the 7th, the 1st to the 6th are disabled so 6 days
        cut.FindAll(".drp-day[disabled]").Count.ShouldBe(Today.Day - 1);
    }

    /// <summary>
    /// two clicks pick check in then check out and the field shows the nights
    /// </summary>
    [Fact]
    public void PickCheckInThenCheckOut()
    {
        // arrange, these vars play the role of the page, they get the values sent by the callbacks
        DateOnly? checkIn = null;
        DateOnly? checkOut = null;
        var cut = Render<DateRangePicker>(p => p
            .Add(x => x.CheckInChanged, (DateOnly? d) => { checkIn = d; })
            .Add(x => x.CheckOutChanged, (DateOnly? d) => { checkOut = d; }));

        // act, open the calendar then click in 2 days and in 5 days
        cut.Find(".drp-field").Click();
        cut.Find(DaySelector(Today.AddDays(2))).Click();
        cut.Find(DaySelector(Today.AddDays(5))).Click();

        // assert, the page got both dates through the callbacks
        checkIn.ShouldBe(Today.AddDays(2));
        checkOut.ShouldBe(Today.AddDays(5));

        // and the field shows the nb of nights, 5 minus 2 is 3
        cut.Find(".drp-value").TextContent.ShouldContain("3 nights");
    }

    /// <summary>
    /// clicking a day before the check in starts a new range
    /// </summary>
    [Fact]
    public void RestartRangeWhenClickingBeforeCheckIn()
    {
        // arrange, only the check in is watched here
        DateOnly? checkIn = null;
        var cut = Render<DateRangePicker>(p => p
            .Add(x => x.CheckInChanged, (DateOnly? d) => { checkIn = d; }));

        // act, pick in 5 days then click an earlier day, in 2 days
        cut.Find(".drp-field").Click();
        cut.Find(DaySelector(Today.AddDays(5))).Click();
        cut.Find(DaySelector(Today.AddDays(2))).Click();

        // assert, the earlier day became the new check in
        checkIn.ShouldBe(Today.AddDays(2));
    }

    /// <summary>
    /// months start under the right weekday, monday first
    /// </summary>
    [Fact]
    public void StartMonthsOnTheRightWeekday()
    {
        // arrange and act, DaysOf is public static so it can be tested without rendering
        // 1 october 2026 is a thursday so 3 empty slots for mo tu we
        var days = DateRangePicker.DaysOf(new DateOnly(2026, 10, 1)).ToList();

        // assert, the 3 first cells are empty
        days.Take(3).ShouldAllBe(d => d == null);

        // the 4th cell is the 1st, under thursday
        days[3].ShouldBe(new DateOnly(2026, 10, 1));

        // and october has 31 real days
        days.Count(d => d != null).ShouldBe(31);
    }
}
