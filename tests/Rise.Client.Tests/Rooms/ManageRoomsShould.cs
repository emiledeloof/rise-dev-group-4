using Ardalis.Result;
using NSubstitute;
using Rise.Shared.Identity;
using Rise.Shared.Rooms;
using Rise.Shared.RoomTypes;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Rooms;

/// <summary>
/// tests for the admin page <see cref="ManageRooms"/>, the api is replaced by substitutes
/// </summary>
public class ManageRoomsShould : BunitContext
{
    private readonly IRoomAdminService _roomAdminService = Substitute.For<IRoomAdminService>();
    private readonly IRoomTypeService _roomTypeService = Substitute.For<IRoomTypeService>();

    public ManageRoomsShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton(_roomAdminService);
        Services.AddSingleton(_roomTypeService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);

        // default answers, a test overrides them when it needs something else
        RoomsAre(Room(1, "101", "Deluxe Room"), Room(2, "102", null));
        _roomTypeService.GetIndexAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<RoomTypeResponse.Index>.Success(new RoomTypeResponse.Index
            {
                RoomTypes = [new RoomTypeDto.Index { Id = 3, Name = "Deluxe Room" }],
            })));
    }

    private static RoomAdminDto.Index Room(int id, string number, string? typeName) => new()
    {
        Id = id,
        RoomNumber = number,
        RoomTypeName = typeName,
        MaxGuests = 2,
    };

    private void RoomsAre(params RoomAdminDto.Index[] rooms)
    {
        _roomAdminService.GetIndexAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<RoomAdminDto.Index>>.Success(rooms)));
    }

    // opens the form and types a room, the submit is left to the test
    private static void FillTheForm(IRenderedComponent<ManageRooms> cut, string number)
    {
        cut.Find(".room-heading button").Click();
        cut.Find("#room-number").Change(number);
        cut.Find("#room-type").Change("3");
        cut.Find("#max-guests").Change("2");
    }

    [Fact]
    public void ShowEachRoomWithItsType()
    {
        var cut = Render<ManageRooms>();

        var rows = cut.FindAll(".room-table tbody tr");
        rows.Count.ShouldBe(2);
        rows[0].TextContent.ShouldContain("101");
        rows[0].TextContent.ShouldContain("Deluxe Room");
        // * an old room without type shows a dash instead of an empty cell
        rows[1].TextContent.ShouldContain("—");
    }

    [Fact]
    public void ShowAMessageWhenThereAreNoRooms()
    {
        RoomsAre();

        var cut = Render<ManageRooms>();

        cut.Find(".room-muted").TextContent.ShouldBe("No rooms yet.");
    }

    [Fact]
    public void ShowAnErrorInsteadOfAnEmptyListWhenTheRoomsCannotBeLoaded()
    {
        _roomAdminService.GetIndexAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<RoomAdminDto.Index>>.Error("api down")));

        var cut = Render<ManageRooms>();

        cut.Find(".room-form-error").TextContent.ShouldBe("The rooms could not be loaded.");
        cut.FindAll(".room-muted").ShouldBeEmpty();
        cut.FindAll(".room-heading button").ShouldBeEmpty();
    }

    [Fact]
    public void HideTheNewRoomButtonWhenTheRoomTypesCannotBeLoaded()
    {
        _roomTypeService.GetIndexAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<RoomTypeResponse.Index>.Error("api down")));

        var cut = Render<ManageRooms>();

        cut.Find(".room-form-error").TextContent.ShouldBe("The room types could not be loaded.");
        cut.FindAll(".room-heading button").ShouldBeEmpty();
    }

    [Fact]
    public void NotCallTheApiWhenTheFormIsInvalid()
    {
        var cut = Render<ManageRooms>();
        cut.Find(".room-heading button").Click();

        cut.Find(".room-panel form").Submit();

        cut.Find(".room-panel").TextContent.ShouldContain("Room number is required.");
        _roomAdminService.DidNotReceive().CreateAsync(Arg.Any<RoomRequest.Create>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateARoomAndHighlightItInTheList()
    {
        _roomAdminService.CreateAsync(Arg.Any<RoomRequest.Create>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Success(4)));
        var cut = Render<ManageRooms>();
        // the list is read again after the save, now with the new room
        RoomsAre(Room(1, "101", "Deluxe Room"), Room(4, "104", "Deluxe Room"));

        FillTheForm(cut, " 104 ");
        cut.Find(".room-panel form").Submit();

        _roomAdminService.Received(1).CreateAsync(
            Arg.Is<RoomRequest.Create>(r => r.RoomNumber == " 104 " && r.RoomTypeId == 3 && r.MaxGuests == 2),
            Arg.Any<CancellationToken>());
        cut.Find(".room-success").TextContent.ShouldBe("Room 104 created.");
        cut.Find(".room-row-created").TextContent.ShouldContain("104");
        cut.FindAll(".room-panel").ShouldBeEmpty();
    }

    [Fact]
    public void ShowTheConflictAndHighlightTheRoomThatHasTheNumber()
    {
        _roomAdminService.CreateAsync(Arg.Any<RoomRequest.Create>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Conflict("Room 101 already exists.")));
        var cut = Render<ManageRooms>();

        FillTheForm(cut, "101");
        cut.Find(".room-panel form").Submit();

        // ! the form stays open so the admin can change the number
        cut.Find(".room-panel .room-form-error").TextContent.ShouldBe("Room 101 already exists.");
        cut.Find(".room-row-conflict").TextContent.ShouldContain("101");
    }

    [Fact]
    public void ClearTheConflictAsSoonAsAFieldChanges()
    {
        _roomAdminService.CreateAsync(Arg.Any<RoomRequest.Create>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Conflict("Room 101 already exists.")));
        var cut = Render<ManageRooms>();
        FillTheForm(cut, "101");
        cut.Find(".room-panel form").Submit();

        cut.Find("#room-number").Change("105");

        cut.FindAll(".room-panel .room-form-error").ShouldBeEmpty();
        cut.FindAll(".room-row-conflict").ShouldBeEmpty();
    }
}
