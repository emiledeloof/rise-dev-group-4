using Ardalis.Result;
using Microsoft.AspNetCore.Components.Web;
using Rise.Client.Components;
using Rise.Shared.Bookings;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

public class BookingPopupShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public BookingPopupShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void NotRenderAnythingWhenBookingIdIsNull()
    {
        var cut = Render<BookingPopup>(p => p
            .Add(x => x.BookingId, null)
            .Add(x => x.X, 100)
            .Add(x => x.Y, 200));

        cut.FindAll(".booking-popup").Count.ShouldBe(0);
    }

    [Fact]
    public void RenderBookingDetailsWhenFetchSucceeds()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.Full());

        var cut = Render<BookingPopup>(p => p
            .Add(x => x.BookingId, 7)
            .Add(x => x.X, 150)
            .Add(x => x.Y, 250));

        cut.Find(".booking-popup").ShouldNotBeNull();
        cut.Find(".popup-booking-number").TextContent.ShouldBe("20261004-SVM-000007");
        cut.Find(".popup-status .tag.is-primary").TextContent.Trim().ShouldBe("Paid");
        cut.Find(".popup-guest-name").TextContent.ShouldBe("Lukas Müller");
        cut.Find(".popup-guest-email").TextContent.ShouldContain("lukas.mueller@example.com");
        cut.Find(".popup-guest-phone").TextContent.ShouldContain("+49 151 23456789");
        cut.Find(".popup-checkin").TextContent.ShouldBe("17 Oct 2026");
        cut.Find(".popup-checkout").TextContent.ShouldBe("20 Oct 2026");
        cut.Find(".popup-nights").TextContent.ShouldBe("(3 nights)");
        cut.Find(".popup-rooms-list").TextContent.ShouldBe("101, 103");
    }

    [Fact]
    public void RenderPendingStatusWhenBookingIsPending()
    {
        var booking = TestAdminDetails.Full();
        booking.Status = "Pending";
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = Render<BookingPopup>(p => p
            .Add(x => x.BookingId, 7)
            .Add(x => x.X, 100)
            .Add(x => x.Y, 100));

        cut.Find(".popup-status .tag.is-warning").TextContent.Trim().ShouldBe("Pending");
    }

    [Fact]
    public void RenderEmptyStateWhenFetchFails()
    {
        _bookingService.AdminDetailAnswer = Result.NotFound();

        var cut = Render<BookingPopup>(p => p
            .Add(x => x.BookingId, 999)
            .Add(x => x.X, 100)
            .Add(x => x.Y, 100));

        cut.Find(".booking-popup").ShouldNotBeNull();
        cut.Find(".popup-empty-state").ShouldNotBeNull();
        cut.Find(".popup-empty-title").TextContent.ShouldBe("Details unavailable");
        cut.FindAll(".popup-booking-number").Count.ShouldBe(0);
        cut.FindAll(".popup-guest-name").Count.ShouldBe(0);
    }
}
