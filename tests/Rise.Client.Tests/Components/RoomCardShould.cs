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
    // the 1st is non refundable, the 2nd refundable with the client rules
    private static RateDto.Available[] SampleRates(int nights = 3) =>
    [
        new()
        {
            Id = 1, Name = "Non-refundable", Description = "No refund after booking", PricePerNight = 109m, TotalPrice = 109m * nights,
            IsRefundable = false, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50,
        },
        new()
        {
            Id = 2, Name = "Fully refundable", Description = null, PricePerNight = 139m, TotalPrice = 139m * nights,
            IsRefundable = true, FullRefundDays = 30, PartialRefundDays = 7, PartialRefundPercent = 50,
        },
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
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("3 nights · prices include VAT when enabled");
    }

    /// <summary>
    /// one line per rate with its total and price per night, no rate name and no conditions
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
        // the name tells the rates apart, the conditions are the policy line
        first.QuerySelector(".room-card-rate-name")!.TextContent.ShouldBe("Non-refundable");
        lines[1].QuerySelector(".room-card-rate-name")!.TextContent.ShouldBe("Fully refundable");
        first.QuerySelector(".room-card-rate-conditions").ShouldBeNull();
        first.QuerySelector(".room-card-total")!.TextContent.ShouldBe("€327.00");
        first.QuerySelector(".room-card-rate-detail")!.TextContent.ShouldBe("€109.00 × 3 nights");
    }

    /// <summary>
    /// night is singular for a 1 night stay
    /// </summary>
    /// <summary>
    /// each rate shows its cancellation policy under the price
    /// </summary>
    [Fact]
    public void ShowTheCancellationPolicyOfEachRate()
    {
        // arrange and act
        var cut = RenderCard();

        // assert
        var policies = cut.FindAll(".room-card-rate-policy");
        policies.Count.ShouldBe(2);
        policies[0].TextContent.ShouldBe("Non-refundable, no refund if you cancel");
        policies[1].TextContent.ShouldBe("Free cancellation up to 30 days before arrival, 50% refund up to 7 days before");
    }

    [Fact]
    public void WriteNightInSingularForOneNight()
    {
        // arrange and act
        var cut = RenderCard(nights: 1);

        // assert, in the line above the rates and in the detail of each rate
        cut.Find(".room-card-price-detail").TextContent.ShouldBe("1 night · prices include VAT when enabled");
        cut.Find(".room-card-rate-detail").TextContent.ShouldBe("€109.00 × 1 night");
    }

    /// <summary>
    /// a details url turns the name into a link and adds a view details link
    /// </summary>
    [Fact]
    public void LinkToTheDetailsWhenADetailsUrlIsGiven()
    {
        // arrange and act
        var cut = Render<RoomCard>(p => p
            .Add(x => x.Name, "Double Room")
            .Add(x => x.DetailsUrl, "rooms/1"));

        // assert
        cut.Find(".room-card-title-link").GetAttribute("href").ShouldBe("rooms/1");
        cut.Find(".room-card-details-link").GetAttribute("href").ShouldBe("rooms/1");
    }

    /// <summary>
    /// no details url means no link
    /// </summary>
    [Fact]
    public void HideTheDetailsLinkWithoutADetailsUrl()
    {
        // arrange and act
        var cut = RenderCard();

        // assert
        cut.FindAll("a").ShouldBeEmpty();
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

    // renders a card with 2 rates, remembers what the card tells the page
    private IRenderedComponent<RoomCard> RenderSelectableCard(List<int> picked, Action onRemove, int? selectedRateId = null,
        RateDto.Available[]? rates = null) =>
        Render<RoomCard>(p => p
            .Add(x => x.Name, "Double Room")
            .Add(x => x.Nights, 3)
            .Add(x => x.Rates, rates ?? SampleRates())
            .Add(x => x.SelectedRateId, selectedRateId)
            .Add(x => x.OnRateSelected, rateId => picked.Add(rateId))
            .Add(x => x.OnRemove, onRemove));

    /// <summary>
    /// select picks the cheapest rate, the 1st one of the list
    /// </summary>
    [Fact]
    public void PickTheCheapestRateWithSelect()
    {
        // arrange, picked collects the rate ids the card sends to the page
        var picked = new List<int>();
        var cut = RenderSelectableCard(picked, () => { });

        // act
        cut.Find(".select-btn").Click();

        // assert, rate 1 is the non-refundable one, the cheapest
        picked.ShouldBe(new[] { 1 });
    }

    /// <summary>
    /// a click on the radio of a rate sends its id to the page
    /// </summary>
    [Fact]
    public void PickARateWithItsRadio()
    {
        // arrange
        var picked = new List<int>();
        var cut = RenderSelectableCard(picked, () => { });

        // act, Change simulates the radio being checked
        cut.FindAll(".room-card-rate-radio")[1].Change(true);

        // assert, rate 2 is the fully refundable one
        picked.ShouldBe(new[] { 2 });
    }

    /// <summary>
    /// the rate of the selection is the checked radio
    /// </summary>
    [Fact]
    public void CheckTheSelectedRate()
    {
        // arrange and act, the page says rate 2 is chosen for this room
        var cut = RenderSelectableCard([], () => { }, selectedRateId: 2);

        // assert, only the 2nd radio has the checked attr
        var radios = cut.FindAll(".room-card-rate-radio");
        radios[0].HasAttribute("checked").ShouldBeFalse();
        radios[1].HasAttribute("checked").ShouldBeTrue();
    }

    /// <summary>
    /// a room in the selection shows remove instead of select, and remove calls OnRemove
    /// </summary>
    [Fact]
    public void ShowRemoveWhenTheRoomIsSelected()
    {
        // arrange, a counter that goes up each time OnRemove is called
        var removed = 0;
        var cut = RenderSelectableCard([], () => removed++, selectedRateId: 1);

        // act
        var button = cut.Find(".select-btn");
        button.TextContent.Trim().ShouldBe("Remove");
        button.Click();

        // assert
        removed.ShouldBe(1);
    }

    /// <summary>
    /// a room without rates can't be selected, so no select btn
    /// </summary>
    [Fact]
    public void HideSelectWhenTheRoomHasNoRates()
    {
        // arrange and act
        var cut = RenderSelectableCard([], () => { }, rates: []);

        // assert
        cut.FindAll(".select-btn").ShouldBeEmpty();
    }

    // the 2 sample rates for 3 nights, the 1st one needing firstMinNights and the 2nd one secondMinNights
    private static RateDto.Available[] RatesWithMinNights(int firstMinNights, int secondMinNights)
    {
        var rates = SampleRates();
        rates[0].MinNights = firstMinNights;
        rates[1].MinNights = secondMinNights;
        return rates;
    }

    /// <summary>
    /// a rate with a minimum the stay meets shows it under the policy
    /// </summary>
    [Fact]
    public void ShowTheMinStayOfARate()
    {
        // arrange and act, 3 nights, the 2nd rate needs 2
        var cut = RenderSelectableCard([], () => { }, rates: RatesWithMinNights(1, 2));

        // assert, only the 2nd line has a min stay line
        var lines = cut.FindAll(".room-card-rate");
        lines[0].QuerySelector(".room-card-rate-min-stay").ShouldBeNull();
        lines[1].QuerySelector(".room-card-rate-min-stay")!.TextContent.ShouldBe("Minimum stay 2 nights");
    }

    /// <summary>
    /// rates without minimum get no min stay line at all
    /// </summary>
    [Fact]
    public void HideTheMinStayLineForOneNight()
    {
        // arrange and act
        var cut = RenderCard();

        // assert
        cut.FindAll(".room-card-rate-min-stay").ShouldBeEmpty();
    }

    /// <summary>
    /// a rate that needs more nights than the stay is greyed out, disabled and never sent to the page
    /// </summary>
    [Fact]
    public void DisableARateThatNeedsALongerStay()
    {
        // arrange, 3 nights, the 2nd rate needs 5
        var picked = new List<int>();
        var cut = RenderSelectableCard(picked, () => { }, rates: RatesWithMinNights(1, 5));

        // act
        cut.FindAll(".room-card-rate-radio")[1].Change(true);

        // assert
        var line = cut.FindAll(".room-card-rate")[1];
        line.ClassList.ShouldContain("is-unavailable");
        line.QuerySelector(".room-card-rate-radio")!.HasAttribute("disabled").ShouldBeTrue();
        line.QuerySelector(".room-card-rate-min-stay")!.TextContent.ShouldBe("Requires a stay of at least 5 nights");
        picked.ShouldBeEmpty();
    }

    /// <summary>
    /// select skips a rate that needs a longer stay, even when the server sends it 1st
    /// </summary>
    [Fact]
    public void PickTheCheapestBookableRateWithSelect()
    {
        // arrange, the 1st rate needs 5 nights so only the 2nd one can be booked
        var picked = new List<int>();
        var cut = RenderSelectableCard(picked, () => { }, rates: RatesWithMinNights(5, 1));

        // act
        cut.Find(".select-btn").Click();

        // assert
        picked.ShouldBe(new[] { 2 });
    }

    /// <summary>
    /// a room whose rates all need a longer stay can't be selected
    /// </summary>
    [Fact]
    public void HideSelectWhenNoRateIsBookable()
    {
        // arrange and act
        var cut = RenderSelectableCard([], () => { }, rates: RatesWithMinNights(5, 5));

        // assert, the rates are still shown so the guest sees why
        cut.FindAll(".room-card-rate").Count.ShouldBe(2);
        cut.FindAll(".select-btn").ShouldBeEmpty();
    }
}
