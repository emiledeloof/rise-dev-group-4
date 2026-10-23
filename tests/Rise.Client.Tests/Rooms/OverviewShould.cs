using Ardalis.Result;
using Rise.Client.Search;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Rooms;

/// <summary>
/// tests for the rooms overview page, see Rooms/Overview.razor
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class OverviewShould : BunitContext
{
    // fake api, the page gets it instead of the real RoomService
    private readonly FakeRoomService _roomService = new();

    /// <summary>
    /// sets up the logger and plugs the fake room service
    /// </summary>
    public OverviewShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRoomService>(_roomService);
    }

    // shortcut to build a room, only the fields a test cares about
    private static RoomDto.Summary Room(int id, decimal? fromPrice = 100m, int maxGuests = 2) => new()
    {
        Id = id,
        Name = $"Room {id}",
        Description = "A room.",
        MaxGuests = maxGuests,
        FromPrice = fromPrice,
        PhotoUrls = [$"images/rooms/room-{id}.jpg"],
    };

    // fake answer with the given rooms, the total is the nb of rooms
    private void GivenRooms(params RoomDto.Summary[] rooms) =>
        _roomService.ListAnswer = Result.Success(new RoomResponse.List { Rooms = rooms, TotalCount = rooms.Length });

    /// <summary>
    /// the page asks for as many rooms as the list validator allows, not only the 3 of the landing page
    /// </summary>
    [Fact]
    public void AskForUpToTwentyRooms()
    {
        // act
        Render<Overview>();

        // assert
        _roomService.ListRequests.Single().Take.ShouldBe(20);
    }

    /// <summary>
    /// each row of the list links to the detail page of its own room
    /// </summary>
    [Fact]
    public void ListEveryRoomWithALinkToItsDetailPage()
    {
        // act, the default fake has 3 rooms
        var cut = Render<Overview>();

        // assert
        cut.FindAll(".rooms-row")
            .Select(a => a.GetAttribute("href"))
            .ShouldBe(["rooms/1", "rooms/2", "rooms/3"]);
        cut.FindAll(".rooms-row-name")
            .Select(n => n.TextContent)
            .ShouldBe(["Standard Room", "Deluxe Room", "Deluxe Room with bathtub"]);
    }

    /// <summary>
    /// only the first rooms get a big block, but every room is in the list
    /// </summary>
    [Fact]
    public void ShowAtMostThreeFeaturedRooms()
    {
        // arrange
        GivenRooms(Room(1), Room(2), Room(3), Room(4), Room(5));

        // act
        var cut = Render<Overview>();

        // assert
        cut.FindAll(".rooms-feature").Count.ShouldBe(3);
        cut.FindAll(".rooms-row").Count.ShouldBe(5);
    }

    /// <summary>
    /// the welcome note is static, so it is there even when the rooms could not be loaded
    /// </summary>
    [Fact]
    public void ShowTheWelcomeNoteEvenWhenTheRoomsFail()
    {
        // arrange
        _roomService.ListAnswer = Result.Error("boom");

        // act
        var cut = Render<Overview>();

        // assert
        cut.Find(".rooms-welcome-text").TextContent.ShouldContain("stazzu");
    }

    /// <summary>
    /// the overview shows no prices, they depend on the dates and are on the results page
    /// </summary>
    [Fact]
    public void ShowNoPrices()
    {
        // act, the rooms of the default fake all have a from price
        var cut = Render<Overview>();

        // assert, \u20AC is the euro sign, escaped so the test does not depend on the file encoding
        cut.Markup.ShouldNotContain("\u20AC");
        cut.Markup.ShouldNotContain("per night");
    }

    /// <summary>
    /// the nb of photos of a room is not shown
    /// </summary>
    [Fact]
    public void ShowNoPhotoCount()
    {
        // act, the standard room of the default fake has 2 photos
        var cut = Render<Overview>();

        // assert
        cut.Markup.ShouldNotContain("photos</span>");
    }

    /// <summary>
    /// each photo of the mosaic opens the room it belongs to
    /// </summary>
    [Fact]
    public void LinkEachMosaicPhotoToItsRoom()
    {
        // act
        var cut = Render<Overview>();

        // assert, 1st photo of each room first, then the 2nd photo of the standard room
        cut.FindAll(".rooms-mosaic-tile")
            .Select(a => a.GetAttribute("href"))
            .ShouldBe(["rooms/1", "rooms/2", "rooms/3", "rooms/1"]);
    }

    /// <summary>
    /// the api failed, a msg with a way to the search replaces the rooms
    /// </summary>
    [Fact]
    public void ShowAMessageWhenTheRoomsCannotBeLoaded()
    {
        // arrange
        _roomService.ListAnswer = Result.Error("boom");

        // act
        var cut = Render<Overview>();

        // assert
        cut.Find(".rooms-empty").TextContent.ShouldContain("Our rooms could not be loaded right now.");
        cut.FindAll(".rooms-row").ShouldBeEmpty();
    }

    /// <summary>
    /// no room in the db, its own msg so the page never stays blank and does not look like an error
    /// </summary>
    [Fact]
    public void ShowAMessageWhenThereAreNoRooms()
    {
        // arrange
        GivenRooms();

        // act
        var cut = Render<Overview>();

        // assert
        var message = cut.Find(".rooms-empty").TextContent;
        message.ShouldContain("Our rooms are being prepared, please come back soon.");
        message.ShouldNotContain("could not be loaded");
    }
}
