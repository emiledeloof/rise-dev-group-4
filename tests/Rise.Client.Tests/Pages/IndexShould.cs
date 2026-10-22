using Ardalis.Result;
using Rise.Client.Search;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Pages;

/// <summary>
/// These tests are written entirely in C#.
/// Learn more at https://bunit.dev/docs/getting-started/writing-tests.html#creating-basic-tests-in-cs-files
/// </summary>
public class IndexShould : BunitContext
{
    // fake api, the page gets it instead of the real RoomService
    private readonly FakeRoomService _roomService = new();

    public IndexShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRoomService>(_roomService);
    }

    // shortcut to build a card room, only the fields a test cares about
    private static RoomDto.Summary Room(int id, string? description = "A room.", decimal? fromPrice = 100m) => new()
    {
        Id = id,
        Name = $"Room {id}",
        Description = description,
        MaxGuests = 2,
        FromPrice = fromPrice,
    };

    /// <summary>
    /// the hero shows the title of the mockup, it replaced the hello world of the template
    /// </summary>
    [Fact]
    public void ShowTheHeroTitle()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert
        cut.Find("h1").TextContent.ShouldBe("Slow days among vines and granite");
    }

    /// <summary>
    /// the hero holds the shared search bar, so the guest can search a stay from the home
    /// </summary>
    [Fact]
    public void ShowTheSearchBar()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert, one search bar inside the hero
        cut.FindAll(".hero .search-bar").Count.ShouldBe(1);
    }

    /// <summary>
    /// each room card links to the detail page of its own room, not to the room list
    /// </summary>
    [Fact]
    public void LinkEachRoomCardToItsDetailPage()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert, the ids of the rooms of the fake in order
        cut.FindAll(".room-card .home-link")
            .Select(a => a.GetAttribute("href"))
            .ShouldBe(["rooms/1", "rooms/2", "rooms/3"]);
    }

    /// <summary>
    /// the cards show the name, short text and from price of the rooms of the api
    /// </summary>
    [Fact]
    public void ShowTheRoomsFromTheApi()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".room-card h3").Select(h => h.TextContent)
            .ShouldBe(["Standard Room", "Deluxe Room", "Deluxe Room with bathtub"]);
        cut.FindAll(".room-card-description")[0].TextContent.ShouldBe("Cosy room with a view on the vines.");
        cut.FindAll(".room-card-price")[0].TextContent.ShouldContain("from €133 per night");
    }

    /// <summary>
    /// the page asks the api for at most 3 rooms, one row of cards
    /// </summary>
    [Fact]
    public void AskForAtMostThreeRooms()
    {
        // Act
        Render<Index>();

        // Assert
        _roomService.ListRequests.Single().Take.ShouldBe(3);
    }

    /// <summary>
    /// the db has more rooms than the cards, a link leads to the others
    /// </summary>
    [Fact]
    public void ShowSeeAllRoomsWhenMoreRoomsExist()
    {
        // Arrange
        _roomService.ListAnswer = Result.Success(new RoomResponse.List { Rooms = [Room(1), Room(2), Room(3)], TotalCount = 5 });

        // Act
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".rooms-all").Count.ShouldBe(1);
    }

    /// <summary>
    /// all the rooms are already on the cards, no link to see more
    /// </summary>
    [Fact]
    public void HideSeeAllRoomsWhenAllRoomsAreShown()
    {
        // Act, the default fake has 3 rooms and a total of 3
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".rooms-all").ShouldBeEmpty();
    }

    /// <summary>
    /// a room without a description gets no empty text line
    /// </summary>
    [Fact]
    public void HideTheDescriptionWhenARoomHasNone()
    {
        // Arrange
        _roomService.ListAnswer = Result.Success(new RoomResponse.List { Rooms = [Room(1, description: null)], TotalCount = 1 });

        // Act
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".room-card-description").ShouldBeEmpty();
    }

    /// <summary>
    /// a room without a rate shows no price instead of €0
    /// </summary>
    [Fact]
    public void HideThePriceWhenARoomHasNoRate()
    {
        // Arrange
        _roomService.ListAnswer = Result.Success(new RoomResponse.List { Rooms = [Room(1, fromPrice: null)], TotalCount = 1 });

        // Act
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".room-card-price").ShouldBeEmpty();
    }

    /// <summary>
    /// the api failed, a msg replaces the cards and the rest of the page still shows
    /// </summary>
    [Fact]
    public void ShowAMessageWhenTheRoomsCannotBeLoaded()
    {
        // Arrange
        _roomService.ListAnswer = Result.Error("The server answered 500.");

        // Act
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".room-card").ShouldBeEmpty();
        cut.Find(".rooms-empty").TextContent.ShouldContain("Our rooms could not be loaded right now.");
        cut.Find("h1").TextContent.ShouldBe("Slow days among vines and granite");
    }

    /// <summary>
    /// a click on a photo of the estate shows it large
    /// </summary>
    [Fact]
    public void OpenTheEstatePhotoLarge()
    {
        // Arrange, the lightbox takes the focus when it opens
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<Index>();

        // Act
        cut.Find(".estate-photo .carousel-zoom").Click();

        // Assert
        cut.FindAll(".lightbox").Count.ShouldBe(1);
    }

    /// <summary>
    /// the photos of the room cards stay a plain slider, the detail page has the large photos
    /// </summary>
    [Fact]
    public void NotOpenTheRoomCardPhotosLarge()
    {
        // Act
        var cut = Render<Index>();

        // Assert
        cut.FindAll(".room-card .carousel-zoom").ShouldBeEmpty();
    }
}
