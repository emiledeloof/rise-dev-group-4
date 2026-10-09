using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the <see cref="RoomCard"/> shown in the search results
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class RoomCardShould : BunitContext
{
    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public RoomCardShould(ITestOutputHelper outputHelper)
    {
        // sends the app logs to the test output, handy when a test fails
        Services.AddXunitLogger(outputHelper);
    }

    // renders a card with sample values, each test only changes what it checks
    // 109 per night for 3 nights by default, so a total of 327
    private IRenderedComponent<RoomCard> RenderCard(int maxGuests = 2, string? photoUrl = null, string? description = "Spacious room.",
        int nights = 3, decimal totalPrice = 327m) =>
        Render<RoomCard>(p => p
            .Add(x => x.Name, "Double Room")
            .Add(x => x.Description, description)
            .Add(x => x.MaxGuests, maxGuests)
            .Add(x => x.PricePerNight, 109m)
            .Add(x => x.Nights, nights)
            .Add(x => x.TotalPrice, totalPrice)
            .Add(x => x.PhotoUrl, photoUrl));

    /// <summary>
    /// name and description are shown
    /// </summary>
    [Fact]
    public void ShowTheRoomDetails()
    {
        // arrange and act
        var cut = RenderCard();

        // assert, Markup is the html of the card as text
        cut.Find("h3").TextContent.ShouldBe("Double Room");
        cut.Find(".room-card-description").TextContent.ShouldBe("Spacious room.");
    }

    /// <summary>
    /// the price block shows the price per night, the nb of nights and the total
    /// </summary>
    [Fact]
    public void ShowThePriceBreakdown()
    {
        // arrange and act, 109 per night for 3 nights
        var cut = RenderCard();

        // assert, the detail line then the total with the vat note
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("€109 × 3 nights");
        cut.Find(".room-card-total").TextContent.ShouldBe("€327 total incl. VAT");
    }

    /// <summary>
    /// night is singular for a 1 night stay
    /// </summary>
    [Fact]
    public void WriteNightInSingularForOneNight()
    {
        // arrange and act, 1 night so the total is the price of 1 night
        var cut = RenderCard(nights: 1, totalPrice: 109m);

        // assert
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("€109 × 1 night");
    }

    /// <summary>
    /// no description means no empty paragraph
    /// </summary>
    [Fact]
    public void HideTheDescriptionWhenThereIsNone()
    {
        // arrange and act
        var cut = RenderCard(description: null);

        // assert
        cut.FindAll(".room-card-description").ShouldBeEmpty();
    }

    /// <summary>
    /// guest is singular for 1 person and plural for more
    /// </summary>
    [Theory]
    [InlineData(1, "Up to 1 guest")]
    [InlineData(2, "Up to 2 guests")]
    public void WriteGuestsInSingularOrPlural(int maxGuests, string expected)
    {
        // arrange and act
        var cut = RenderCard(maxGuests: maxGuests);

        // assert
        cut.Find(".room-card-meta").TextContent.ShouldBe(expected);
    }

    /// <summary>
    /// a photo url gives an img with an alt text for screen readers
    /// </summary>
    [Fact]
    public void ShowThePhotoWithAnAltText()
    {
        // arrange and act
        var cut = RenderCard(photoUrl: "https://example.com/double.jpg");

        // assert
        var img = cut.Find("img");
        img.GetAttribute("src").ShouldBe("https://example.com/double.jpg");
        img.GetAttribute("alt").ShouldBe("Photo of the Double Room");
    }

    /// <summary>
    /// no photo gives the beige placeholder block instead of a broken img
    /// </summary>
    [Fact]
    public void ShowAPlaceholderWhenThereIsNoPhoto()
    {
        // arrange and act
        var cut = RenderCard(photoUrl: null);

        // assert
        cut.FindAll("img").ShouldBeEmpty();
        cut.FindAll(".is-placeholder").Count.ShouldBe(1);
    }

    /// <summary>
    /// the select btn calls OnSelect
    /// </summary>
    [Fact]
    public void CallOnSelectWhenSelectIsClicked()
    {
        // arrange, a counter that goes up each time OnSelect is called
        var clicks = 0;
        var cut = Render<RoomCard>(p => p
            .Add(x => x.Name, "Double Room")
            .Add(x => x.OnSelect, () => { clicks++; }));

        // act
        cut.Find("button").Click();

        // assert
        clicks.ShouldBe(1);
    }
}
