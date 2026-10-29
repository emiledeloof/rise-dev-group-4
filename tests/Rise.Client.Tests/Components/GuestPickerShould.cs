using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// Component tests for the <see cref="GuestPicker"/> component using bUnit.
/// Verifies initial display, popover open/close, increment/decrement behavior for adults and rooms,
/// fluid auto-adjustments (at least 1 adult per room), and notice displays.
/// </summary>
public class GuestPickerShould : BunitContext
{
    public GuestPickerShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    [Fact]
    public void DisplayDefaultAdultsCount()
    {
        // Act
        var cut = Render<GuestPicker>();

        // Assert
        cut.Find(".gp-label").TextContent.Trim().ShouldBe("Guests");
        cut.Find(".gp-value").TextContent.Trim().ShouldBe("2 Adults");
        cut.FindAll(".gp-panel").ShouldBeEmpty();
    }

    [Fact]
    public void DisplaySingularForOneAdult()
    {
        // Act
        var cut = Render<GuestPicker>(p => p.Add(x => x.Value, 1));

        // Assert
        cut.Find(".gp-value").TextContent.Trim().ShouldBe("1 Adult");
    }

    [Fact]
    public void OpenPanelWhenFieldClicked()
    {
        // Arrange
        var cut = Render<GuestPicker>();

        // Act
        cut.Find(".gp-field").Click();

        // Assert
        cut.Find(".gp-panel").ShouldNotBeNull();
        cut.Find(".gp-field").GetAttribute("aria-expanded").ShouldBe("true");
        cut.Find(".gp-count").TextContent.Trim().ShouldBe("2");
    }

    [Fact]
    public void IncreaseAdultsCountWhenPlusClicked()
    {
        // Arrange
        var changedValue = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 1)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedValue = v)));

        cut.Find(".gp-field").Click();

        // Act
        var plusBtn = cut.Find("button[aria-label='Increase adults']");
        plusBtn.Click();

        // Assert
        changedValue.ShouldBe(2);
    }

    [Fact]
    public void DecreaseAdultsCountWhenMinusClicked()
    {
        // Arrange
        var changedValue = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedValue = v)));

        cut.Find(".gp-field").Click();

        // Act
        var minusBtn = cut.Find("button[aria-label='Decrease adults']");
        minusBtn.Click();

        // Assert
        changedValue.ShouldBe(1);
    }

    [Fact]
    public void DisableMinusButtonWhenAtMinimum()
    {
        // Arrange
        var cut = Render<GuestPicker>(p => p.Add(x => x.Value, 1));
        cut.Find(".gp-field").Click();

        // Act & Assert
        var minusBtn = cut.Find("button[aria-label='Decrease adults']");
        minusBtn.HasAttribute("disabled").ShouldBeTrue();

        var plusBtn = cut.Find("button[aria-label='Increase adults']");
        plusBtn.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void DisablePlusButtonWhenAtMaximum()
    {
        // Arrange
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Max, 6)
            .Add(x => x.Value, 6));
        cut.Find(".gp-field").Click();

        // Act & Assert
        var plusBtn = cut.Find("button[aria-label='Increase adults']");
        plusBtn.HasAttribute("disabled").ShouldBeTrue();

        var minusBtn = cut.Find("button[aria-label='Decrease adults']");
        minusBtn.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ClosePanelWhenDoneClicked()
    {
        // Arrange
        var cut = Render<GuestPicker>();
        cut.Find(".gp-field").Click();
        cut.Find(".gp-panel").ShouldNotBeNull();

        // Act
        cut.Find(".gp-done-btn").Click();

        // Assert
        cut.FindAll(".gp-panel").ShouldBeEmpty();
        cut.Find(".gp-field").GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void DisplayGuestsAndRoomsWhenRoomsProvided()
    {
        // Act
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 1));

        // Assert
        cut.Find(".gp-label").TextContent.Trim().ShouldBe("Guests & Rooms");
        cut.Find(".gp-value").TextContent.Trim().ShouldBe("2 Adults · 1 Room");
    }

    [Fact]
    public void DisplayPluralRooms()
    {
        // Act
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 4)
            .Add(x => x.Rooms, 2));

        // Assert
        cut.Find(".gp-value").TextContent.Trim().ShouldBe("4 Adults · 2 Rooms");
    }

    [Fact]
    public void IncreaseRoomsWhenPlusRoomsClicked()
    {
        // Arrange
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 1)
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var plusRoomsBtn = cut.Find("button[aria-label='Increase rooms']");
        plusRoomsBtn.Click();

        // Assert
        changedRooms.ShouldBe(2);
    }

    [Fact]
    public void DecreaseRoomsWhenMinusRoomsClicked()
    {
        // Arrange
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 2)
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var minusRoomsBtn = cut.Find("button[aria-label='Decrease rooms']");
        minusRoomsBtn.Click();

        // Assert
        changedRooms.ShouldBe(1);
    }

    [Fact]
    public void DisableMinusRoomsWhenAtMinimum()
    {
        // Arrange
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 1));

        cut.Find(".gp-field").Click();

        // Act & Assert
        var minusBtn = cut.Find("button[aria-label='Decrease rooms']");
        minusBtn.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void DisablePlusRoomsWhenAtMaximum()
    {
        // Arrange
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.MaxRooms, 3)
            .Add(x => x.Value, 6)
            .Add(x => x.Rooms, 3));

        cut.Find(".gp-field").Click();

        // Act & Assert
        var plusBtn = cut.Find("button[aria-label='Increase rooms']");
        plusBtn.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void AutoIncreaseAdultsWhenIncreasingRoomsAboveAdults()
    {
        // Arrange: 1 adult and 1 room. Increasing room to 2 smoothly raises adults to 2 (at least 1 adult per room).
        var changedAdults = 0;
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 1)
            .Add(x => x.Rooms, 1)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, a => changedAdults = a))
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var plusRoomsBtn = cut.Find("button[aria-label='Increase rooms']");
        plusRoomsBtn.Click();

        // Assert
        changedRooms.ShouldBe(2);
        changedAdults.ShouldBe(2);
        cut.Find(".gp-notice").TextContent.ShouldContain("Guest added (min 1 adult/room)");
    }

    [Fact]
    public void KeepRoomsWhenIncreasingAdultsWithinEstateCapacity()
    {
        // Arrange: 2 adults and 1 room with Max = 6. Increasing adults to 3 does not force an extra room.
        var changedAdults = 0;
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Max, 6)
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 1)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, a => changedAdults = a))
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var plusAdultsBtn = cut.Find("button[aria-label='Increase adults']");
        plusAdultsBtn.Click();

        // Assert
        changedAdults.ShouldBe(3);
        changedRooms.ShouldBe(0); // room count unchanged
        cut.FindAll(".gp-notice").ShouldBeEmpty();
    }

    [Fact]
    public void AutoDecreaseRoomsWhenDecreasingAdultsBelowRoomCount()
    {
        // Arrange: 2 adults and 2 rooms. Decreasing adults to 1 smoothly reduces rooms to 1 (at least 1 adult per room).
        var changedAdults = 0;
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Value, 2)
            .Add(x => x.Rooms, 2)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, a => changedAdults = a))
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var minusAdultsBtn = cut.Find("button[aria-label='Decrease adults']");
        minusAdultsBtn.Click();

        // Assert
        changedAdults.ShouldBe(1);
        changedRooms.ShouldBe(1);
        cut.Find(".gp-notice").TextContent.ShouldContain("Room removed (min 1 adult/room)");
    }

    [Fact]
    public void KeepAdultsWhenDecreasingRoomsAboveGuestCount()
    {
        // Arrange: 4 adults and 2 rooms. Decreasing rooms to 1 does not force adults down because 4 adults >= 1 room.
        var changedAdults = 0;
        var changedRooms = 0;
        var cut = Render<GuestPicker>(p => p
            .Add(x => x.Max, 6)
            .Add(x => x.Value, 4)
            .Add(x => x.Rooms, 2)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, a => changedAdults = a))
            .Add(x => x.RoomsChanged, EventCallback.Factory.Create<int>(this, r => changedRooms = r)));

        cut.Find(".gp-field").Click();

        // Act
        var minusRoomsBtn = cut.Find("button[aria-label='Decrease rooms']");
        minusRoomsBtn.Click();

        // Assert
        changedRooms.ShouldBe(1);
        changedAdults.ShouldBe(0); // adults unchanged
        cut.FindAll(".gp-notice").ShouldBeEmpty();
    }
}
