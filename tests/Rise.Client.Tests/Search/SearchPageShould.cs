using System.Globalization;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Search;

/// <summary>
/// tests for the stay search page, see Search/Index.razor
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class SearchPageShould : BunitContext
{
    // dates are relative to today so the tests keep working whatever day they run
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public SearchPageShould(ITestOutputHelper outputHelper)
    {
        // sends the app logs to the test output, handy when a test fails
        Services.AddXunitLogger(outputHelper);
    }

    // finds a day btn of the calendar by its aria label, same helper as in DateRangePickerShould
    private static string DaySelector(DateOnly day) =>
        $"[aria-label='{day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}']";

    /// <summary>
    /// sending the form without dates shows both error messages
    /// </summary>
    [Fact]
    public void ShowErrorsWhenSearchingWithoutDates()
    {
        // arrange, render the search page, Index here is the one of the Search folder
        var cut = Render<Index>();

        // act, send the form, bunit needs Submit on the form, a click on the btn is not enough
        cut.Find("form").Submit();

        // assert, 2 error messages, one for check in and one for check out
        // they have the has-text-danger class we gave them in the page
        cut.FindAll(".has-text-danger").Count.ShouldBe(2);
    }

    /// <summary>
    /// sending the form with valid dates goes to the results page with the dates in the url
    /// </summary>
    [Fact]
    public void GoToResultsWithTheDatesInTheUrl()
    {
        // arrange, render the search page
        var cut = Render<Index>();

        // act, open the calendar, pick in 2 days then in 5 days, then send the form
        cut.Find(".drp-field").Click();
        cut.Find(DaySelector(Today.AddDays(2))).Click();
        cut.Find(DaySelector(Today.AddDays(5))).Click();
        cut.Find("form").Submit();

        // assert, bunit gives a fake NavigationManager, its Uri is the page we were sent to
        // the url must end with /search/results?checkIn=...&checkOut=... in yyyy-MM-dd
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.Uri.ShouldEndWith($"/search/results?checkIn={Today.AddDays(2):yyyy-MM-dd}&checkOut={Today.AddDays(5):yyyy-MM-dd}");
    }
}
