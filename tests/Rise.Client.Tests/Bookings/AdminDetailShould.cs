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
    public void LinkToTheFinalBill()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-bill-open").GetAttribute("href").ShouldBe("admin/bookings/7/bill");
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

    private static BookingDto.AdminDetail Cancelled(decimal refund)
    {
        var booking = TestAdminDetails.Full();
        booking.Status = "Cancelled";
        booking.Cancellation = new BookingDto.AdminCancellationDto
        {
            CancelledAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            Reason = "Guest is ill",
            RefundAmount = refund,
        };
        return booking;
    }

    [Fact]
    public void PrefillTheRefundOfThePolicyInTheCancelDialog()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-cancel-open").Click();

        cut.Find("#booking-cancel-refund").GetAttribute("value").ShouldBe("360");
        cut.Find(".booking-cancel-refund-hint").TextContent.ShouldContain("€360.00 of €720.00 paid");
        cut.Find(".booking-cancel-dialog .booking-detail-refund-note").TextContent.ShouldContain("does not pay it back");
    }

    [Fact]
    public void NotCancelWithoutReason()
    {
        var cut = RenderPage();
        cut.Find(".booking-detail-cancel-open").Click();

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldBeEmpty();
        cut.Find(".booking-cancel-dialog .help.is-danger").TextContent.ShouldBe("Please enter a reason for the cancellation.");
    }

    [Fact]
    public void CloseTheCancelDialogWhenKeepingTheBooking()
    {
        var cut = RenderPage();
        cut.Find(".booking-detail-cancel-open").Click();

        cut.FindAll(".booking-cancel-actions button")[0].Click();

        cut.FindAll(".booking-cancel-dialog").ShouldBeEmpty();
        _bookingService.CancelRequests.ShouldBeEmpty();
    }

    [Fact]
    public void CancelTheBookingAndAcknowledgeIt()
    {
        var cut = RenderPage();
        cut.Find(".booking-detail-cancel-open").Click();
        cut.Find("#booking-cancel-reason").Change("Guest is ill");
        // the page reloads the booking after cancelling
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        cut.Find(".booking-cancel-dialog form").Submit();

        var request = _bookingService.CancelRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.Reason.ShouldBe("Guest is ill");
        request.RefundAmount.ShouldBe(360m);
        cut.FindAll(".booking-cancel-dialog").ShouldBeEmpty();
        cut.Find(".booking-detail-cancel-success").TextContent.ShouldContain("A refund of €360.00 is recorded");
        cut.Find(".booking-detail-status .tag.is-danger").TextContent.Trim().ShouldBe("Cancelled");
    }

    [Fact]
    public void ShowWhyTheServerRefusedTheCancellation()
    {
        _bookingService.CancelAnswer = Result.Conflict("Booking 20261004-SVM-000007 is already cancelled.");
        var cut = RenderPage();
        cut.Find(".booking-detail-cancel-open").Click();
        cut.Find("#booking-cancel-reason").Change("Guest is ill");

        cut.Find(".booking-cancel-dialog form").Submit();

        cut.Find(".booking-cancel-error").TextContent.ShouldBe("Booking 20261004-SVM-000007 is already cancelled.");
        cut.FindAll(".booking-cancel-dialog").Count.ShouldBe(1);
    }

    [Fact]
    public void ShowTheCancellationAndKeepTheOriginalTerms()
    {
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        var cut = RenderPage();

        cut.Find(".booking-detail-cancelled-at").TextContent.ShouldBe("10 Oct 2026");
        cut.Find(".booking-detail-cancel-reason").TextContent.ShouldBe("Guest is ill");
        cut.Find(".booking-detail-refund").TextContent.ShouldBe("€360.00");
        cut.Find(".booking-detail-cancellation .booking-detail-refund-note").TextContent.ShouldContain("does not refund automatically");
        cut.Find(".booking-detail-total-amount").TextContent.ShouldBe("€720.00");
        cut.FindAll(".booking-detail-room").Count.ShouldBe(2);
        cut.FindAll(".booking-detail-payment").Count.ShouldBe(1);
        cut.FindAll(".booking-detail-cancel-open").ShouldBeEmpty();
    }

    [Fact]
    public void ShowNoRefundForACancellationWithoutRefund()
    {
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(0m));

        var cut = RenderPage();

        cut.Find(".booking-detail-refund").TextContent.ShouldBe("No refund");
        cut.FindAll(".booking-detail-cancellation .booking-detail-refund-note").ShouldBeEmpty();
    }

    [Fact]
    public void ShowTheNbOfGuests()
    {
        var cut = RenderPage();

        cut.Find(".booking-detail-guests-count").TextContent.ShouldBe("3 guests");
    }

    [Fact]
    public void ShowASingleGuest()
    {
        var booking = TestAdminDetails.Full();
        booking.Adults = 1;
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.Find(".booking-detail-guests-count").TextContent.ShouldBe("1 guest");
    }

    [Fact]
    public void ShowTheBookedRateAndPolicyOfEveryRoom()
    {
        var cut = RenderPage();

        var conditions = cut.FindAll(".booking-detail-condition");
        conditions.Count.ShouldBe(2);
        conditions[0].QuerySelector(".booking-detail-rate-name")!.TextContent.ShouldBe("Standard");
        conditions[0].QuerySelector(".booking-detail-policy-tag")!.TextContent.ShouldBe("Refundable");
        conditions[0].QuerySelector(".booking-detail-policy-text")!.TextContent
            .ShouldBe("Free cancellation up to 30 days before arrival, 50% refund up to 7 days before");
        cut.Find(".booking-detail-conditions-note").TextContent.ShouldContain("04 Oct 2026");
    }

    /// <summary>
    /// check-in on 17 oct, 100 pct until 17 sep, 50 pct until 10 oct, the reference date 10 oct is in the 50 pct step
    /// </summary>
    [Fact]
    public void ShowTheRefundStepsWithTheCurrentOneHighlighted()
    {
        var cut = RenderPage();

        var steps = cut.FindAll(".booking-detail-condition")[0].QuerySelectorAll(".booking-detail-timeline li");
        steps.Length.ShouldBe(3);
        steps[0].TextContent.ShouldContain("100%");
        steps[0].TextContent.ShouldContain("17 Sep 2026");
        steps[0].ClassList.ShouldContain("is-past");
        steps[1].TextContent.ShouldContain("50%");
        steps[1].TextContent.ShouldContain("10 Oct 2026");
        steps[1].ClassList.ShouldContain("is-current");
        steps[2].TextContent.ShouldContain("0%");
        steps[2].ClassList.ShouldNotContain("is-current");
    }

    [Fact]
    public void ShowTheRefundIfCancelledToday()
    {
        var cut = RenderPage();

        var entitlement = cut.FindAll(".booking-detail-entitlement")[1];
        entitlement.TextContent.ShouldContain("Refund if cancelled today");
        entitlement.QuerySelector(".booking-detail-entitlement-amount")!.TextContent.ShouldBe("50% · €210.00");
    }

    [Fact]
    public void ShowNoRefundForANonRefundableRate()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.NonRefundable());

        var cut = RenderPage();

        cut.Find(".booking-detail-policy-tag").TextContent.ShouldBe("Non-refundable");
        cut.Find(".booking-detail-policy-text").TextContent.ShouldBe("Non-refundable, no refund if the guest cancels");
        cut.FindAll(".booking-detail-timeline").ShouldBeEmpty();
        cut.Find(".booking-detail-entitlement-amount").TextContent.ShouldBe("0% · €0.00");
    }

    [Fact]
    public void ShowTheEntitlementOnTheCancellationDateOfACancelledBooking()
    {
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        var cut = RenderPage();

        cut.Find(".booking-detail-entitlement").TextContent.ShouldContain("Entitlement on 10 Oct 2026");
        cut.FindAll(".booking-detail-condition").Count.ShouldBe(2);
    }

    [Fact]
    public void WarnAboutAPaymentAfterTheCancellation()
    {
        var booking = Cancelled(0m);
        booking.Payments.Single().PaidAt = booking.Cancellation!.CancelledAt.AddHours(1);
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.Find(".booking-detail-paid-after-cancel").TextContent.ShouldContain("A payment came in after the cancellation");
    }

    [Fact]
    public void NotWarnAboutAPaymentBeforeTheCancellation()
    {
        // paid on 4 Oct, cancelled on 10 Oct with a partial refund
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        var cut = RenderPage();

        cut.FindAll(".booking-detail-paid-after-cancel").ShouldBeEmpty();
    }
}
