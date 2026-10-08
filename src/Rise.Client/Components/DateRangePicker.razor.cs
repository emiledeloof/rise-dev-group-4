using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// calendar to pick a check in and a check out date, built without any library
/// </summary>
/// <example>
/// <code>
/// &lt;DateRangePicker @bind-CheckIn="_stay.CheckIn" @bind-CheckOut="_stay.CheckOut" /&gt;
/// </code>
/// </example>
public partial class DateRangePicker
{
    // TODO: use the selected language for month and day names with the languages task
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    // weeks start on monday like in most of europe
    private static readonly string[] WeekDayNames = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];

    /// <summary>
    /// chosen arrival date, use with @bind-CheckIn
    /// </summary>
    [Parameter] public DateOnly? CheckIn { get; set; }

    /// <summary>
    /// called when the arrival date changes
    /// </summary>
    [Parameter] public EventCallback<DateOnly?> CheckInChanged { get; set; }

    /// <summary>
    /// chosen departure date, use with @bind-CheckOut
    /// </summary>
    [Parameter] public DateOnly? CheckOut { get; set; }

    /// <summary>
    /// called when the departure date changes
    /// </summary>
    [Parameter] public EventCallback<DateOnly?> CheckOutChanged { get; set; }

    /// <summary>
    /// first date that can be picked, today by default
    /// </summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>
    /// nb of months shown side by side, the second one is hidden on small screens
    /// </summary>
    [Parameter] public int MonthsShown { get; set; } = 2;

    private bool _isOpen;
    private DateOnly _firstVisibleMonth;
    private DateOnly? _hoverDate;

    private DateOnly FirstAllowedDate => MinDate ?? DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// starts on the month of the chosen check in, or on the current month
    /// </summary>
    protected override void OnInitialized()
    {
        _firstVisibleMonth = FirstDayOfMonth(CheckIn ?? FirstAllowedDate);
    }

    // ---------- navigation ----------

    private IEnumerable<DateOnly> VisibleMonths =>
        Enumerable.Range(0, MonthsShown).Select(i => _firstVisibleMonth.AddMonths(i));

    // no going back before the month of the first allowed date
    private bool CanGoBack => _firstVisibleMonth > FirstDayOfMonth(FirstAllowedDate);

    private void PreviousMonth()
    {
        if (CanGoBack)
            _firstVisibleMonth = _firstVisibleMonth.AddMonths(-1);
    }

    private void NextMonth() => _firstVisibleMonth = _firstVisibleMonth.AddMonths(1);

    private void Toggle() => _isOpen = !_isOpen;

    // ---------- selection ----------

    /// <summary>
    /// first click picks the check in, second click picks the check out
    /// a click on or before the check in, or after a full range, starts a new range
    /// </summary>
    private async Task SelectAsync(DateOnly day)
    {
        if (IsDisabled(day))
            return;

        var startsNewRange = CheckIn is null || CheckOut is not null || day <= CheckIn;

        if (startsNewRange)
        {
            CheckIn = day;
            CheckOut = null;
            await CheckInChanged.InvokeAsync(CheckIn);
            await CheckOutChanged.InvokeAsync(CheckOut);
            return;
        }

        CheckOut = day;
        await CheckOutChanged.InvokeAsync(CheckOut);

        // range is complete so the calendar can close
        _isOpen = false;
        _hoverDate = null;
    }

    private void Hover(DateOnly day) => _hoverDate = day;

    private void ClearHover() => _hoverDate = null;

    // ---------- display ----------

    private int Nights => CheckIn is { } checkIn && CheckOut is { } checkOut
        ? checkOut.DayNumber - checkIn.DayNumber
        : 0;

    // text in the closed field, eg 14 oct to 17 oct 2026 and 3 nights
    private string FieldText => (CheckIn, CheckOut) switch
    {
        (null, _) => "Add dates",
        ({ } checkIn, null) => $"{checkIn.ToString("d MMM yyyy", Culture)} – …",
        ({ } checkIn, { } checkOut) =>
            $"{checkIn.ToString("d MMM", Culture)} – {checkOut.ToString("d MMM yyyy", Culture)} · {NightsText}",
    };

    // TODO: translate with the languages task
    private string HintText => (CheckIn, CheckOut) switch
    {
        (null, _) => "Select your check-in date",
        (_, null) => "Select your check-out date",
        _ => NightsText,
    };

    private string NightsText => Nights == 1 ? "1 night" : $"{Nights} nights";

    private bool IsDisabled(DateOnly day) => day < FirstAllowedDate;

    private bool IsSelected(DateOnly day) => day == CheckIn || day == CheckOut;

    /// <summary>
    /// css classes of a day, the range end follows the mouse until a check out is picked
    /// </summary>
    private string DayClass(DateOnly day)
    {
        var rangeEnd = CheckOut ?? (_hoverDate > CheckIn ? _hoverDate : null);

        var classes = new List<string> { "drp-day" };

        if (day == DateOnly.FromDateTime(DateTime.Today)) classes.Add("is-today");
        if (IsDisabled(day)) classes.Add("is-disabled");
        if (day == CheckIn) classes.Add("is-start");
        if (day == rangeEnd) classes.Add("is-end");
        if (day > CheckIn && day < rangeEnd) classes.Add("is-in-range");

        return string.Join(" ", classes);
    }

    // ---------- helpers ----------

    /// <summary>
    /// all the cells of a month grid, in order from left to right
    /// returns null for each empty cell before the 1st, then one date per day of the month
    /// eg october 2026 starts on a thursday so it returns null, null, null, 1 oct, 2 oct and so on until 31 oct
    /// </summary>
    /// <param name="month">any date of the month to show, only its year and month are used</param>
    public static IEnumerable<DateOnly?> DaysOf(DateOnly month)
    {
        // step 1, build the date of the 1st of the month, eg 15 oct becomes 1 oct
        var first = FirstDayOfMonth(month);

        // step 2, ask dotnet which weekday the 1st is
        // first.DayOfWeek is an enum, sunday 0, monday 1 and so on until saturday 6
        // (int) turns it into that number so we can do maths with it
        // + 6 then % 7 shifts the week so monday gives 0, eg thursday 4 gives 3 empty cells
        // % 7 is the rest of the division by 7, eg 12 % 7 is 5
        var emptySlots = ((int)first.DayOfWeek + 6) % 7;

        // empty cells push the 1st under the right weekday column
        for (var i = 0; i < emptySlots; i++)
            yield return null;

        // DaysInMonth gives 28 to 31, leap years included
        var daysInMonth = DateTime.DaysInMonth(first.Year, first.Month);

        // then every day of the month, 1st + 0 days, 1st + 1 day and so on
        for (var i = 0; i < daysInMonth; i++)
            yield return first.AddDays(i);
    }

    /// <summary>
    /// builds a new date on the 1st of the same month and year
    /// eg 15 oct 2026 gives 1 oct 2026
    /// </summary>
    /// <param name="date">any date of the month</param>
    private static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}
