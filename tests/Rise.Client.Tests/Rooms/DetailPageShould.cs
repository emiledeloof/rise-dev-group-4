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
        cut.Find(".room-detail-no-description").TextContent.ShouldBe("No description is available for this room yet.");
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

    /// <summary>
    /// the 1st photo is the static hero, the other photos are in the slider of the gallery
    /// </summary>
    [Fact]
    public void ShowTheFirstPhotoAsHeroAndTheOthersInTheGallery()
    {
        // arrange and act, the fake room has 2 photos
        var cut = RenderPage();

        // assert
        var hero = cut.Find(".room-hero-photo");
        hero.GetAttribute("src").ShouldBe("images/rooms/double-1.jpg");
        hero.GetAttribute("alt").ShouldBe("Photo of the Double Room");
        cut.Find(".room-detail-photos .carousel-photo").GetAttribute("src").ShouldBe("images/rooms/double-2.jpg");
        cut.FindAll(".room-detail-no-photos").ShouldBeEmpty();
    }

    /// <summary>
    /// a room with only 1 photo shows it as hero and has no gallery
    /// </summary>
    [Fact]
    public void HideTheGalleryWhenThereIsOnlyTheHeroPhoto()
    {
        // arrange
        _roomService.DetailAnswer = Result.Success(new RoomDto.Detail
        {
            Id = 1,
            Name = "Double Room",
            MaxGuests = 2,
            PhotoUrls = ["images/rooms/double-1.jpg"],
        });

        // act
        var cut = RenderPage();

        // assert
        cut.Find(".room-hero-photo").GetAttribute("src").ShouldBe("images/rooms/double-1.jpg");
        cut.FindAll(".room-detail-photos").ShouldBeEmpty();
        cut.FindAll(".room-detail-no-photos").ShouldBeEmpty();
    }

    /// <summary>
    /// opened from the results, the stay card shows the dates and nights and leads back to the results
    /// </summary>
    [Fact]
    public void ShowTheStayOfTheSearch()
    {
        // arrange and act
        var cut = RenderPage("?checkIn=2026-10-14&checkOut=2026-10-17");

        // assert
        cut.Find(".room-detail-stay-dates").TextContent.ShouldBe("14 Oct – 17 Oct 2026");
        cut.Find(".room-detail-stay-meta").TextContent.ShouldBe("3 nights · up to 2 guests");
        var cta = cut.Find(".room-detail-cta");
        cta.GetAttribute("href").ShouldBe("search/results?checkIn=2026-10-14&checkOut=2026-10-17");
        cta.TextContent.Trim().ShouldBe("See prices for your dates");
    }

    /// <summary>
    /// opened without dates, the stay card invites to check the availability
    /// </summary>
    [Fact]
    public void InviteToCheckAvailabilityWithoutDates()
    {
        // arrange and act
        var cut = RenderPage();

        // assert
        cut.FindAll(".room-detail-stay-dates").ShouldBeEmpty();
        var cta = cut.Find(".room-detail-cta");
        cta.GetAttribute("href").ShouldBe("search");
        cta.TextContent.Trim().ShouldBe("Check availability");
    }

    /// <summary>
    /// a room without photos shows an empty state, not a stock photo
    /// </summary>
    [Fact]
    public void ShowAnEmptyStateWhenThereAreNoPhotos()
    {
        // arrange
        _roomService.DetailAnswer = Result.Success(new RoomDto.Detail
        {
            Id = 1,
            Name = "Double Room",
            Description = "Spacious room with a double bed.",
            MaxGuests = 2,
        });

        // act
        var cut = RenderPage();

        // assert
        cut.FindAll("img").ShouldBeEmpty();
        cut.Find(".room-detail-no-photos").TextContent.ShouldBe("No photos are available for this room yet.");
    }
}
