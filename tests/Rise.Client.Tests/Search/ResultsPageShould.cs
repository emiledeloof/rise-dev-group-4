using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Client.Bookings;
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

    // booking draft the continue btn fills
    private readonly BookingDraft _draft = new();

    /// <summary>
    /// sets up the logger and plugs the fake room service
    /// </summary>
    public ResultsPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);

        // AddSingleton gives this exact object to the page, so the test can read it after
        Services.AddSingleton<IRoomService>(_roomService);
        Services.AddSingleton(_draft);
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
    /// the rates sent by the api reach the cards, the double room has 2 and the single room none
    /// </summary>
    [Fact]
    public void PassTheRatesToTheCards()
    {
        // arrange and act
        var cut = RenderWithDates(2, 5);

        // assert
        cut.FindAll(".room-card-rate").Count.ShouldBe(2);
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
    /// the page sends the adults count from the url to the api
    /// </summary>
    [Fact]
    public void AskTheApiForAdultsOfTheUrl()
    {
        // arrange and act
        Navigation.NavigateTo($"search/results?checkIn={Today.AddDays(2):yyyy-MM-dd}&checkOut={Today.AddDays(5):yyyy-MM-dd}&adults=4");
        Render<Results>();

        // assert
        var request = _roomService.Requests.Last();
        request.Adults.ShouldBe(4);
    }

    /// <summary>
    /// subtitle shows the stay period nights and guest count
    /// </summary>
    [Fact]
    public void ShowGuestCountInSubtitle()
    {
        // arrange and act
        Navigation.NavigateTo($"search/results?checkIn={Today.AddDays(2):yyyy-MM-dd}&checkOut={Today.AddDays(5):yyyy-MM-dd}&adults=3");
        var cut = Render<Results>();

        // assert
        cut.Find("p.subtitle").TextContent.ShouldContain("3 guests");
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

    /// <summary>
    /// nothing is picked when the page opens, the summary shows its hint
    /// </summary>
    [Fact]
    public void StartWithAnEmptySelection()
    {
        // arrange and act
        var cut = RenderWithDates(2, 5);

        // assert
        cut.Find(".selection-summary-empty").TextContent.ShouldBe("Choose a rate on a room to add it here.");
    }

    /// <summary>
    /// select on a card adds the room with its cheapest rate to the summary
    /// </summary>
    [Fact]
    public void AddARoomToTheSummaryWithSelect()
    {
        // arrange, the fake double room has 2 rates, the single room none so only 1 select btn
        var cut = RenderWithDates(2, 5);

        // act
        cut.Find(".select-btn").Click();

        // assert, one line with the cheapest rate and the total of the summary
        var line = cut.Find(".selection-summary-room");
        line.QuerySelector(".selection-summary-room-name")!.TextContent.ShouldBe("Double Room");
        line.QuerySelector(".selection-summary-rate-name")!.TextContent.ShouldBe("Non-refundable");
        cut.Find(".selection-summary-total").TextContent.ShouldContain("€327");
    }

    /// <summary>
    /// picking another rate changes the line and the total, the room is not added twice
    /// </summary>
    [Fact]
    public void ChangeTheRateOfARoom()
    {
        // arrange, the room is in the selection with its cheapest rate
        var cut = RenderWithDates(2, 5);
        cut.Find(".select-btn").Click();

        // act, pick the fully refundable rate
        cut.FindAll(".room-card-rate-radio")[1].Change(true);

        // assert
        cut.FindAll(".selection-summary-room").Count.ShouldBe(1);
        cut.Find(".selection-summary-rate-name").TextContent.ShouldBe("Fully refundable");
        cut.Find(".selection-summary-total").TextContent.ShouldContain("€417");
    }

    /// <summary>
    /// remove takes the room out of the summary
    /// </summary>
    [Fact]
    public void RemoveARoomFromTheSummary()
    {
        // arrange
        var cut = RenderWithDates(2, 5);
        cut.Find(".select-btn").Click();

        // act, the same btn now says remove, Find again because the card was redrawn
        cut.Find(".select-btn").Click();

        // assert
        cut.FindAll(".selection-summary-room").ShouldBeEmpty();
        cut.Find(".selection-summary-empty");
    }

    /// <summary>
    /// new dates empty the selection, the old prices are not valid anymore
    /// </summary>
    [Fact]
    public void EmptyTheSelectionWhenTheDatesChange()
    {
        // arrange, one room picked
        var cut = RenderWithDates(2, 5);
        cut.Find(".select-btn").Click();

        // act, the search bar sends the guest to the same page with other dates
        Navigation.NavigateTo($"search/results?checkIn={Today.AddDays(10):yyyy-MM-dd}&checkOut={Today.AddDays(12):yyyy-MM-dd}");

        // assert, retried a bit because the page reloads on its own
        cut.WaitForAssertion(() => cut.FindAll(".selection-summary-room").ShouldBeEmpty());
    }

    /// <summary>
    /// continue stores the stay and the picked room with its rate in the draft, then goes to the guest details
    /// </summary>
    [Fact]
    public void StoreTheSelectionInTheDraftOnContinue()
    {
        // arrange, the double room with its fully refundable rate
        var cut = RenderWithDates(2, 5);
        cut.Find(".select-btn").Click();
        cut.FindAll(".room-card-rate-radio")[1].Change(true);

        // act
        cut.Find(".selection-summary button").Click();

        // assert
        _draft.CheckIn.ShouldBe(Today.AddDays(2));
        _draft.CheckOut.ShouldBe(Today.AddDays(5));
        var room = _draft.Rooms.Single();
        room.RoomId.ShouldBe(1);
        room.RateId.ShouldBe(2);
        room.TotalPrice.ShouldBe(417m);
        // * the policy text is kept for the overview
        room.CancellationText.ShouldBe("Free cancellation up to 30 days before arrival, 50% refund up to 7 days before");
        Navigation.Uri.ShouldEndWith("/guests/create");
    }

    /// <summary>
    /// back from the next steps with the same dates, the rooms picked before are selected again
    /// </summary>
    [Fact]
    public void RestoreTheSelectionOfTheDraftForTheSameDates()
    {
        // arrange
        _draft.SetRooms(Today.AddDays(2), Today.AddDays(5),
            [new DraftRoom(1, "Double Room", 2, "Fully refundable", null, 139m, 417m, "Free cancellation up to 30 days before arrival, 50% refund up to 7 days before")]);

        // act
        var cut = RenderWithDates(2, 5);

        // assert
        cut.Find(".selection-summary-rate-name").TextContent.ShouldBe("Fully refundable");
    }

    /// <summary>
    /// other dates mean other prices, the draft selection is not restored
    /// </summary>
    [Fact]
    public void NotRestoreTheSelectionOfTheDraftForOtherDates()
    {
        // arrange
        _draft.SetRooms(Today.AddDays(10), Today.AddDays(12),
            [new DraftRoom(1, "Double Room", 2, "Fully refundable", null, 139m, 278m, "Free cancellation up to 30 days before arrival, 50% refund up to 7 days before")]);

        // act
        var cut = RenderWithDates(2, 5);

        // assert
        cut.Find(".selection-summary-empty");
    }
}
