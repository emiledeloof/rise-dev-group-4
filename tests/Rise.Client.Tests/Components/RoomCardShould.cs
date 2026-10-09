using Rise.Shared.Rooms;
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

    // 2 sample rates for 3 nights, the 2nd one has no conditions on purpose
    private static RateDto.Available[] SampleRates(int nights = 3) =>
    [
        new() { Id = 1, Name = "Non-refundable", Description = "No refund after booking", PricePerNight = 109m, TotalPrice = 109m * nights },
        new() { Id = 2, Name = "Fully refundable", Description = null, PricePerNight = 139m, TotalPrice = 139m * nights },
    ];

    // renders a card with sample values, each test only changes what it checks
    private IRenderedComponent<RoomCard> RenderCard(int maxGuests = 2, string? photoUrl = null, string? description = "Spacious room.",
        int nights = 3) =>
        Render<RoomCard>(p => p
            .Add(x => x.Name, "Double Room")
            .Add(x => x.Description, description)
            .Add(x => x.MaxGuests, maxGuests)
            .Add(x => x.PricePerNight, 109m)
            .Add(x => x.Nights, nights)
            .Add(x => x.TotalPrice, 109m * nights)
            .Add(x => x.Rates, SampleRates(nights))
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
    /// the nb of nights is written once above the rates, with the vat note
    /// </summary>
    [Fact]
    public void ShowTheNightsOnceAboveTheRates()
    {
        // arrange and act
        var cut = RenderCard();

        // assert
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("3 nights · prices include VAT");
    }

    /// <summary>
    /// one line per rate with its name, conditions, total and price per night
    /// </summary>
    [Fact]
    public void ShowOneLinePerRate()
    {
        // arrange and act
        var cut = RenderCard();

        // assert, 2 rates so 2 lines, the 1st one checked in detail
        var lines = cut.FindAll(".room-card-rate");
        lines.Count.ShouldBe(2);

        var first = lines[0];
        first.QuerySelector(".room-card-rate-name")!.TextContent.ShouldBe("Non-refundable");
        first.QuerySelector(".room-card-rate-conditions")!.TextContent.ShouldBe("No refund after booking");
        first.QuerySelector(".room-card-total")!.TextContent.ShouldBe("€327");
        first.QuerySelector(".room-card-rate-detail")!.TextContent.ShouldBe("€109 × 3 nights");
    }

    /// <summary>
    /// a rate without conditions has no empty conditions line
    /// </summary>
    [Fact]
    public void HideTheConditionsWhenARateHasNone()
    {
        // arrange and act
        var cut = RenderCard();

        // assert, QuerySelector gives null when nothing matches inside the 2nd line
        cut.FindAll(".room-card-rate")[1].QuerySelector(".room-card-rate-conditions").ShouldBeNull();
    }

    /// <summary>
    /// night is singular for a 1 night stay
    /// </summary>
    [Fact]
    public void WriteNightInSingularForOneNight()
    {
        // arrange and act
        var cut = RenderCard(nights: 1);

        // assert, in the line above the rates and in the detail of each rate
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("1 night · prices include VAT");
        cut.Find(".room-card-rate-detail").TextContent.ShouldBe("€109 × 1 night");
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
