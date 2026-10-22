using Ardalis.Result;
using Rise.Shared.Bookings;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

/// <summary>
/// Tests for the admin booking detail page, see Bookings/AdminDetail.razor.
/// Each test follows 3 steps: arrange, act, assert.
/// </summary>
public class AdminDetailShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public AdminDetailShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    private IRenderedComponent<AdminDetail> RenderPage()
        => Render<AdminDetail>(p => p.Add(x => x.BookingId, 7));

    [Fact]
    public void ShowBookingNumberAndStatus()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-number").TextContent.ShouldBe("20261004-SVM-000007");
        cut.Find(".booking-detail-status .tag.is-primary").TextContent.Trim().ShouldBe("Paid");
        cut.Find(".booking-detail-created").TextContent.ShouldBe("Booked on 04 Oct 2026");
    }

    [Fact]
    public void ShowPendingStatusForUnpaidBooking()
    {
        var booking = TestAdminDetails.Full();
        booking.Status = "Pending";
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.Find(".booking-detail-status .tag.is-warning").TextContent.Trim().ShouldBe("Pending");
    }

    [Fact]
    public void ShowStayDatesAndNights()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-checkin").TextContent.ShouldBe("17 Oct 2026");
        cut.Find(".booking-detail-checkout").TextContent.ShouldBe("20 Oct 2026");
        cut.Find(".booking-detail-nights-count").TextContent.ShouldBe("3 nights");
    }

    [Fact]
    public void ShowAssignedRooms()
    {
        var cut = RenderPage();

        var rooms = cut.FindAll(".booking-detail-room");
        rooms.Count.ShouldBe(2);
        rooms[0].QuerySelector(".booking-detail-room-number")!.TextContent.ShouldBe("101");
        rooms[0].QuerySelector(".booking-detail-room-name")!.TextContent.ShouldBe("Standard Room");
        rooms[1].TextContent.ShouldContain("Flexible");
    }

    [Fact]
    public void ShowPricePerRoomAndTotal()
    {
        var cut = RenderPage();

        cut.FindAll(".booking-detail-price-row").Count.ShouldBe(2);
        cut.Find(".booking-detail-total-amount").TextContent.ShouldBe("€720.00");
    }

    [Fact]
    public void ShowGuestContactDetails()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-guest-name").TextContent.ShouldBe("Lukas Müller");
        cut.Find(".booking-detail-email a").GetAttribute("href").ShouldBe("mailto:lukas.mueller@example.com");
        cut.Find(".booking-detail-phone a").GetAttribute("href").ShouldBe("tel:+49 151 23456789");
        var address = cut.Find(".booking-detail-address").TextContent;
        address.ShouldContain("Hauptstraße 5");
        address.ShouldContain("2. Stock");
        address.ShouldContain("Germany");
    }

    [Fact]
    public void ShowEmptyStateWhenNoRooms()
    {
        var booking = TestAdminDetails.Full();
        booking.Rooms = [];
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.FindAll(".booking-detail-room").ShouldBeEmpty();
        cut.Find(".booking-detail-no-rooms").TextContent.ShouldBe("No rooms are assigned to this booking.");
    }

    [Fact]
    public void ShowPaymentDetails()
    {
        var cut = RenderPage();

        var payment = cut.Find(".booking-detail-payment");
        payment.TextContent.ShouldContain("Paid 04 Oct 2026");
        payment.TextContent.ShouldContain("INV-2026-0002");
        payment.TextContent.ShouldContain("Visa •••• 4242");
    }

    [Fact]
    public void ShowEmptyStateWhenNoPayments()
    {
        var booking = TestAdminDetails.Full();
        booking.Payments = [];
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.FindAll(".booking-detail-payment").ShouldBeEmpty();
        cut.Find(".booking-detail-no-payments").TextContent.ShouldBe("No payments recorded yet.");
    }

    [Fact]
    public void ShowNotFoundMessage()
    {
        _bookingService.AdminDetailAnswer = Result.NotFound();

        var cut = RenderPage();

        cut.Find(".booking-detail-error").TextContent.ShouldBe("This booking could not be found.");
        cut.FindAll(".booking-detail-number").ShouldBeEmpty();
    }

    [Fact]
    public void ShowErrorMessage()
    {
        _bookingService.AdminDetailAnswer = Result.Error();

        var cut = RenderPage();

        cut.Find(".booking-detail-error").TextContent.ShouldBe("Sorry, we could not load this booking. Please try again later.");
    }

    [Fact]
    public void LinkBackToTheBookingList()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-back").GetAttribute("href").ShouldBe("admin/booking-list");
    }
}
