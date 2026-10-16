using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Client.Search;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Rooms;

/// <summary>
/// tests for the room detail page, see Rooms/Detail.razor
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class DetailPageShould : BunitContext
{
    // fake api, the page gets it instead of the real RoomService
    private readonly FakeRoomService _roomService = new();

    /// <summary>
    /// sets up the logger and plugs the fake room service
    /// </summary>
    public DetailPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRoomService>(_roomService);
    }

    // bunit fake NavigationManager, its Uri is the current page
    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // opens the detail page of room 1, with an optional query string like from the results
    private IRenderedComponent<Detail> RenderPage(string query = "")
    {
        Navigation.NavigateTo($"rooms/1{query}");
        return Render<Detail>(p => p.Add(x => x.RoomId, 1));
    }

    /// <summary>
    /// the name, capacity and description of the room are shown
    /// </summary>
    [Fact]
    public void ShowTheRoomDescription()
    {
        // arrange and act
        var cut = RenderPage();

        // assert
        cut.Find("h1").TextContent.ShouldBe("Double Room");
        cut.Find(".room-detail-meta").TextContent.ShouldBe("Up to 2 guests");
        cut.Find(".room-detail-description").TextContent.ShouldBe("Spacious room with a double bed.");
    }

    /// <summary>
    /// a room without description shows an empty state, not a made up text
    /// </summary>
    [Fact]
    public void ShowAnEmptyStateWhenThereIsNoDescription()
    {
        // arrange
        _roomService.DetailAnswer = Result.Success(new RoomDto.Detail { Id = 1, Name = "Double Room", MaxGuests = 2 });

        // act
        var cut = RenderPage();

        // assert
        cut.FindAll(".room-detail-description").ShouldBeEmpty();
        cut.Find(".room-detail-empty").TextContent.ShouldBe("No description is available for this room yet.");
    }

    /// <summary>
    /// an unknown room shows a not found msg and no room info
    /// </summary>
    [Fact]
    public void ShowNotFoundForAnUnknownRoom()
    {
        // arrange
        _roomService.DetailAnswer = Result.NotFound();

        // act
        var cut = RenderPage();

        // assert
        cut.FindAll("h1").ShouldBeEmpty();
        cut.Find(".room-detail-empty").TextContent.ShouldBe("This room could not be found.");
    }

    /// <summary>
    /// opened from the results, the back btn returns to the results of the same dates
    /// </summary>
    [Fact]
    public void GoBackToTheResultsOfTheSameDates()
    {
        // arrange and act
        var cut = RenderPage("?checkIn=2026-10-14&checkOut=2026-10-17");

        // assert
        var back = cut.Find(".back-btn");
        back.GetAttribute("href").ShouldBe("search/results?checkIn=2026-10-14&checkOut=2026-10-17");
        back.TextContent.Trim().ShouldBe("Back to available rooms");
    }

    /// <summary>
    /// opened without dates, the back btn goes to the search form
    /// </summary>
    [Fact]
    public void GoToTheSearchWithoutDates()
    {
        // arrange and act
        var cut = RenderPage();

        // assert
        cut.Find(".back-btn").GetAttribute("href").ShouldBe("search");
    }
}
