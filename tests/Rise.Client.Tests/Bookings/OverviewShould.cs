using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

/// <summary>
/// tests for the booking overview page, see Bookings/Overview.razor
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class OverviewShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public OverviewShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // plugs the draft and opens the overview
    private IRenderedComponent<Overview> RenderWith(BookingDraft draft)
    {
        Services.AddSingleton(draft);
        Navigation.NavigateTo("booking/overview");
        return Render<Overview>();
    }

    [Fact]
    public void GoBackToTheSearchWhenNoRoomIsPicked()
    {
        // arrange and act, eg after a page refresh
        RenderWith(new BookingDraft());

        // assert
        Navigation.Uri.ShouldEndWith("/search");
    }

    [Fact]
    public void GoBackToTheGuestFormWhenTheDetailsAreMissing()
    {
        // arrange and act
        RenderWith(TestDrafts.WithRooms());

        // assert
        Navigation.Uri.ShouldEndWith("/guests/create");
    }

    [Fact]
    public void ShowTheStayTheRoomsAndTheGuest()
    {
        // arrange and act
        var cut = RenderWith(TestDrafts.WithRooms(withGuest: true));

        // assert
        cut.Find(".overview-stay").TextContent.ShouldContain("3 nights");
        cut.Find(".overview-room-name").TextContent.ShouldBe("Double Room");
        cut.Find(".overview-rate-name").TextContent.ShouldContain("Non-refundable");
        cut.Find(".overview-rate-description").TextContent.ShouldBe("No refund after booking");
        cut.Find(".overview-room-price").TextContent.ShouldBe("€327");
        cut.Find(".overview-guest").TextContent.ShouldContain("Marie Peeters");
        cut.Find(".overview-guest").TextContent.ShouldContain("marie@example.com");
        cut.Find(".overview-total").TextContent.ShouldContain("€327");
    }

    [Fact]
    public void LinkTheRoomsEditBackToTheResultsOfTheSameDates()
    {
        // arrange and act
        var cut = RenderWith(TestDrafts.WithRooms(withGuest: true));

        // assert
        cut.Find(".overview-rooms .overview-edit").GetAttribute("href")
            .ShouldBe($"/search/results?checkIn={TestDrafts.CheckIn:yyyy-MM-dd}&checkOut={TestDrafts.CheckOut:yyyy-MM-dd}");
    }

    [Fact]
    public void SaveTheBookingAndOpenStripeCheckoutOnPayNow()
    {
        // arrange
        var draft = TestDrafts.WithRooms(withGuest: true);
        var cut = RenderWith(draft);

        // act
        cut.Find(".pay-btn").Click();

        // assert, the request holds the picked room and rate, the checkout is for the saved booking, the draft is emptied
        var request = _bookingService.CreateRequests.Single();
        request.CheckIn.ShouldBe(TestDrafts.CheckIn);
        request.Rooms.Single().RoomId.ShouldBe(1);
        request.Rooms.Single().RateId.ShouldBe(1);
        request.Guest.Email.ShouldBe("marie@example.com");
        _bookingService.CheckoutRequests.Single().BookingId.ShouldBe(1);
        draft.HasRooms.ShouldBeFalse();
        Navigation.Uri.ShouldBe("https://checkout.stripe.com/c/pay/cs_test_123");
    }

    [Fact]
    public void ShowAnErrorAndKeepTheDraftWhenTheCheckoutFails()
    {
        // arrange
        _bookingService.CheckoutAnswer = Result.Error();
        var draft = TestDrafts.WithRooms(withGuest: true);
        var cut = RenderWith(draft);

        // act
        cut.Find(".pay-btn").Click();

        // assert, the guest stays on the page and can try again
        cut.Find(".overview-error").TextContent.ShouldContain("could not start the payment");
        draft.HasRooms.ShouldBeTrue();
        Navigation.Uri.ShouldEndWith("/booking/overview");
    }

    [Fact]
    public void NotSaveTheBookingTwiceWhenRetryingTheCheckout()
    {
        // arrange, the 1st checkout fails
        _bookingService.CheckoutAnswer = Result.Error();
        var cut = RenderWith(TestDrafts.WithRooms(withGuest: true));
        cut.Find(".pay-btn").Click();

        // act, the 2nd one works
        _bookingService.CheckoutAnswer = Result.Success(new BookingResponse.CheckoutSession
        {
            CheckoutUrl = "https://checkout.stripe.com/c/pay/cs_test_456",
            SessionId = "cs_test_456",
        });
        cut.Find(".pay-btn").Click();

        // assert, the booking is saved once and the checkout is asked twice for it
        _bookingService.CreateRequests.Count.ShouldBe(1);
        _bookingService.CheckoutRequests.Count.ShouldBe(2);
        _bookingService.CheckoutRequests.ShouldAllBe(r => r.BookingId == 1);
        Navigation.Uri.ShouldBe("https://checkout.stripe.com/c/pay/cs_test_456");
    }

    [Fact]
    public void ShowAMessageWhenARoomIsNoLongerAvailable()
    {
        // arrange
        _bookingService.CreateAnswer = Result.Conflict("Double Room is no longer available for these dates.");
        var draft = TestDrafts.WithRooms(withGuest: true);
        var cut = RenderWith(draft);

        // act
        cut.Find(".pay-btn").Click();

        // assert, the guest stays on the page and keeps the draft
        cut.Find(".overview-error").TextContent.ShouldContain("Double Room is no longer available");
        cut.Find(".overview-error a").TextContent.Trim().ShouldBe("Choose other rooms");
        draft.HasRooms.ShouldBeTrue();
        Navigation.Uri.ShouldEndWith("/booking/overview");
    }

    [Fact]
    public void ShowTheErrorsWhenTheBookingIsInvalid()
    {
        // arrange
        _bookingService.CreateAnswer = Result.Invalid(new ValidationError("Check-in can't be in the past."));
        var cut = RenderWith(TestDrafts.WithRooms(withGuest: true));

        // act
        cut.Find(".pay-btn").Click();

        // assert
        cut.Find(".overview-error").TextContent.ShouldContain("Check-in can't be in the past.");
    }
}
