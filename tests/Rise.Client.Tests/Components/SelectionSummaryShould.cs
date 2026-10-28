using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the <see cref="SelectionSummary"/> of the results page
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class SelectionSummaryShould : BunitContext
{
    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public SelectionSummaryShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    // renders the summary with the given rooms for a 3 nights stay
    private IRenderedComponent<SelectionSummary> RenderSummary(params SelectedRoom[] rooms) =>
        Render<SelectionSummary>(p => p
            .Add(x => x.Rooms, rooms)
            .Add(x => x.Nights, 3));

    /// <summary>
    /// nothing picked shows the hint, no lines and no total
    /// </summary>
    [Fact]
    public void ShowAHintWhenNothingIsPicked()
    {
        // arrange and act
        var cut = RenderSummary();

        // assert
        cut.Find(".selection-summary-empty").TextContent.ShouldBe("Choose a rate on a room to add it here.");
        cut.FindAll(".selection-summary-total").ShouldBeEmpty();
    }

    /// <summary>
    /// one line per picked room with its name, rate and price
    /// </summary>
    [Fact]
    public void ShowOneLinePerPickedRoom()
    {
        // arrange and act
        var cut = RenderSummary(
            new SelectedRoom("Double Room", "Non-refundable", 327m),
            new SelectedRoom("Single Room", "Fully refundable", 297m));

        // assert, 2 lines, the 1st one checked in detail
        var lines = cut.FindAll(".selection-summary-room");
        lines.Count.ShouldBe(2);
        lines[0].QuerySelector(".selection-summary-room-name")!.TextContent.ShouldBe("Double Room");
        lines[0].QuerySelector(".selection-summary-rate-name")!.TextContent.ShouldBe("Non-refundable");
        lines[0].QuerySelector(".selection-summary-price")!.TextContent.ShouldBe("€327.00");
    }

    /// <summary>
    /// the total adds up the price of every picked room
    /// </summary>
    [Fact]
    public void AddUpTheTotalOfAllRooms()
    {
        // arrange and act, 327 + 297 = 624
        var cut = RenderSummary(
            new SelectedRoom("Double Room", "Non-refundable", 327m),
            new SelectedRoom("Single Room", "Fully refundable", 297m));

        // assert
        cut.Find(".selection-summary-total").TextContent.ShouldContain("€624.00");
    }

    /// <summary>
    /// the nb of nights is shown under the title
    /// </summary>
    [Fact]
    public void ShowTheNbOfNights()
    {
        // arrange and act
        var cut = RenderSummary();

        // assert
        cut.Find(".selection-summary-meta").TextContent.ShouldBe("3 nights");
    }

    /// <summary>
    /// the continue button is disabled when no room is selected
    /// </summary>
    [Fact]
    public void DisableContinueButtonWhenNoRoomsSelected()
    {
        // arrange and act
        var cut = RenderSummary();

        // assert
        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// the continue button tells the page, the page stores the selection and moves on
    /// </summary>
    [Fact]
    public void CallOnContinueWhenRoomsSelected()
    {
        // arrange
        var continued = false;
        var cut = Render<SelectionSummary>(p => p
            .Add(x => x.Rooms, [new SelectedRoom("Double Room", "Standard", 150m)])
            .Add(x => x.Nights, 3)
            .Add(x => x.OnContinue, () => continued = true));

        // act
        cut.Find("button").Click();

        // assert
        continued.ShouldBeTrue();
    }
}
