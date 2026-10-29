using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Client.Search;
using Rise.Shared.Rooms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// Component tests for <see cref="SearchBar"/> ensuring popup mutual exclusion
/// and dynamic capacity/room count propagation.
/// </summary>
public class SearchBarShould : BunitContext
{
    private readonly FakeRoomService _roomService = new();

    public SearchBarShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRoomService>(_roomService);
    }

    [Fact]
    public void NotShowAnyDropdownInitially()
    {
        // Act
        var cut = Render<SearchBar>();

        // Assert
        cut.FindAll(".drp-panel").ShouldBeEmpty();
        cut.FindAll(".gp-panel").ShouldBeEmpty();
    }

    [Fact]
    public void CloseCalendarWhenGuestPickerIsOpened()
    {
        // Arrange
        var cut = Render<SearchBar>();

        // Open calendar
        cut.Find(".drp-field").Click();
        cut.FindAll(".drp-panel").Count.ShouldBe(1);
        cut.FindAll(".gp-panel").ShouldBeEmpty();

        // Act: click guest picker
        cut.Find(".gp-field").Click();

        // Assert: calendar closed, guest picker open
        cut.FindAll(".drp-panel").ShouldBeEmpty();
        cut.FindAll(".gp-panel").Count.ShouldBe(1);
    }

    [Fact]
    public void CloseGuestPickerWhenCalendarIsOpened()
    {
        // Arrange
        var cut = Render<SearchBar>();

        // Open guest picker
        cut.Find(".gp-field").Click();
        cut.FindAll(".gp-panel").Count.ShouldBe(1);
        cut.FindAll(".drp-panel").ShouldBeEmpty();

        // Act: click calendar
        cut.Find(".drp-field").Click();

        // Assert: guest picker closed, calendar open
        cut.FindAll(".gp-panel").ShouldBeEmpty();
        cut.FindAll(".drp-panel").Count.ShouldBe(1);
    }

    [Fact]
    public void PropagateDynamicCapacitiesToGuestPicker()
    {
        // Act
        var cut = Render<SearchBar>(p => p
            .Add(x => x.MaxGuests, 10)
            .Add(x => x.MaxRooms, 5));

        // Open guest picker
        cut.Find(".gp-field").Click();

        // Assert
        cut.FindAll(".gp-subtitle")[1].TextContent.ShouldContain("Up to 5 rooms");
    }
}
