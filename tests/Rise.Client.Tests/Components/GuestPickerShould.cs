using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// Component tests for the <see cref="GuestPicker"/> component using bUnit.
/// Verifies initial display, popover open/close, increment/decrement behavior,
/// and disabling of buttons at min/max boundaries.
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
            .Add(x => x.Value, 2)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedValue = v)));

        cut.Find(".gp-field").Click();

        // Act
        var plusBtn = cut.Find("button[aria-label='Increase adults']");
        plusBtn.Click();

        // Assert
        changedValue.ShouldBe(3);
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
        var cut = Render<GuestPicker>(p => p.Add(x => x.Value, 6));
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
}
