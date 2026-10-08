using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Search;

/// <summary>
/// tests for the search results page, see Search/Results.razor
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class ResultsPageShould : BunitContext
{
    // dates are relative to today so the tests keep working whatever day they run
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    // fake api, the page gets it instead of the real RoomService
    private readonly FakeRoomService _roomService = new();

    /// <summary>
    /// sets up the logger and plugs the fake room service
    /// </summary>
    public ResultsPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);

        // AddSingleton gives this exact object to the page, so the test can read it after
        Services.AddSingleton<IRoomService>(_roomService);
    }

    // bunit fake NavigationManager, its Uri is the current page
    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // opens the results page with the dates in the url, like after a search
    private IRenderedComponent<Results> RenderWithDates(int checkInInDays, int checkOutInDays)
    {
        Navigation.NavigateTo($"search/results?checkIn={Today.AddDays(checkInInDays):yyyy-MM-dd}&checkOut={Today.AddDays(checkOutInDays):yyyy-MM-dd}");
        return Render<Results>();
    }

    /// <summary>
    /// one card per room sent by the api
    /// </summary>
    [Fact]
    public void ShowOneCardPerFreeRoom()
    {
        // arrange and act
        var cut = RenderWithDates(2, 5);

        // assert, the fake sends 2 rooms by default
        cut.FindAll(".room-card").Count.ShouldBe(2);
        cut.Markup.ShouldContain("Double Room");
    }

    /// <summary>
    /// the page sends the dates of the url to the api
    /// </summary>
    [Fact]
    public void AskTheApiForTheDatesOfTheUrl()
    {
        // arrange and act
        RenderWithDates(2, 5);

        // assert, Last is the most recent call
        var request = _roomService.Requests.Last();
        request.CheckIn.ShouldBe(Today.AddDays(2));
        request.CheckOut.ShouldBe(Today.AddDays(5));
    }

    /// <summary>
    /// no free room shows the message and the btn to change the dates
    /// </summary>
    [Fact]
    public void ShowAMessageWhenNoRoomIsFree()
    {
        // arrange, the api answers with an empty list
        _roomService.Answer = Result.Success(new RoomResponse.Available());

        // act
        var cut = RenderWithDates(2, 5);

        // assert
        cut.FindAll(".room-card").ShouldBeEmpty();
        cut.Markup.ShouldContain("No rooms are available for these dates.");
        cut.Find(".back-btn").TextContent.Trim().ShouldBe("Change dates");
    }

    /// <summary>
    /// a failed api call shows the error msg instead of crashing
    /// </summary>
    [Fact]
    public void ShowAnErrorWhenTheApiFails()
    {
        // arrange, the api answers with an error
        _roomService.Answer = Result<RoomResponse.Available>.Error("server down");

        // act
        var cut = RenderWithDates(2, 5);

        // assert, the msg has the has-text-danger class and no card is shown
        cut.Find(".has-text-danger").TextContent.ShouldContain("could not load the rooms");
        cut.FindAll(".room-card").ShouldBeEmpty();
    }

    /// <summary>
    /// no dates in the url sends the guest back to the search form, without calling the api
    /// </summary>
    [Fact]
    public void GoBackToTheSearchWithoutDates()
    {
        // arrange, url without query string
        Navigation.NavigateTo("search/results");

        // act
        Render<Results>();

        // assert
        Navigation.Uri.ShouldEndWith("/search");
        _roomService.Requests.ShouldBeEmpty();
    }

    /// <summary>
    /// new dates in the url reload the rooms, the trap fixed with OnParametersSetAsync
    /// </summary>
    [Fact]
    public void ReloadTheRoomsWhenTheDatesChange()
    {
        // arrange, first search
        var cut = RenderWithDates(2, 5);

        // act, the search bar sends the guest to the same page with other dates
        Navigation.NavigateTo($"search/results?checkIn={Today.AddDays(10):yyyy-MM-dd}&checkOut={Today.AddDays(12):yyyy-MM-dd}");

        // assert, WaitForAssertion retries a bit because the page reloads on its own
        cut.WaitForAssertion(() => _roomService.Requests.Last().CheckIn.ShouldBe(Today.AddDays(10)));
    }
}
