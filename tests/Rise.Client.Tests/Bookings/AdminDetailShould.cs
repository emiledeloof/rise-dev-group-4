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

    private static BookingDto.AdminDetail Cancelled(decimal refund) => TestAdminDetails.Cancelled(refund);

    // opens the dialog of the booking set on the fake service
    private IRenderedComponent<AdminDetail> RenderOpenDialog()
    {
        var cut = RenderPage();
        cut.Find(".booking-detail-cancel-open").Click();
        return cut;
    }

    private static void ToggleRoom(IRenderedComponent<AdminDetail> cut, int index)
        => cut.FindAll(".booking-cancel-room-toggle input")[index].Change(true);

    private static void SelectAllRooms(IRenderedComponent<AdminDetail> cut)
        => cut.Find(".booking-cancel-select-all input").Change(true);

    [Fact]
    public void SelectNothingWhenOpeningTheDialogOfSeveralRooms()
    {
        var cut = RenderOpenDialog();

        cut.FindAll(".booking-cancel-room").Count.ShouldBe(2);
        cut.FindAll(".booking-cancel-room.is-selected").ShouldBeEmpty();
        cut.FindAll(".booking-cancel-room-refund").ShouldBeEmpty();
        cut.Find(".booking-detail-cancel-open").TextContent.Trim().ShouldBe("Cancel rooms");
        cut.Find(".booking-cancel-dialog .booking-detail-refund-note").TextContent.ShouldContain("does not pay it back");
    }

    [Fact]
    public void PreselectTheOnlyRoomOfABooking()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.NonRefundable());

        var cut = RenderOpenDialog();

        cut.FindAll(".booking-cancel-room.is-selected").Count.ShouldBe(1);
        cut.FindAll(".booking-cancel-select-all").ShouldBeEmpty();
        cut.Find(".booking-detail-cancel-open").TextContent.Trim().ShouldBe("Cancel booking");
        cut.Find(".booking-cancel-title").TextContent.ShouldBe("Cancel this booking?");
    }

    /// <summary>
    /// the first room is refundable, the second not, each is prefilled from its own policy
    /// </summary>
    [Fact]
    public void PrefillTheRefundOfTheOwnPolicyOfEachRoom()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.MixedPolicies());
        var cut = RenderOpenDialog();

        cut.FindAll(".booking-cancel-room-due")[0].TextContent.ShouldBe("Policy: 50% · €150.00");
        cut.FindAll(".booking-cancel-room-due")[1].TextContent.ShouldBe("Policy: 0% · €0.00");
        SelectAllRooms(cut);

        cut.Find("#booking-cancel-refund-21").GetAttribute("value").ShouldBe("150");
        cut.Find("#booking-cancel-refund-23").GetAttribute("value").ShouldBe("0");
        cut.Find(".booking-cancel-summary-total").TextContent.ShouldBe("Total refund €150.00 of €720.00 refundable");
    }

    /// <summary>
    /// only 200 is left to refund, the 1st room takes 150 so the 2nd is prefilled with the 50 left instead of its 210
    /// </summary>
    [Fact]
    public void LimitThePrefilledRefundToWhatCanStillBeRefunded()
    {
        var booking = TestAdminDetails.Full();
        booking.PaidAmount = 200m;
        booking.RefundableAmount = 200m;
        _bookingService.AdminDetailAnswer = Result.Success(booking);
        var cut = RenderOpenDialog();

        SelectAllRooms(cut);

        cut.Find("#booking-cancel-refund-21").GetAttribute("value").ShouldBe("150");
        cut.Find("#booking-cancel-refund-23").GetAttribute("value").ShouldBe("50");
        cut.FindAll(".booking-cancel-refund-limit").ShouldBeEmpty();
    }

    [Fact]
    public void DisableTheRefundWhenNothingWasPaid()
    {
        var booking = TestAdminDetails.Full();
        booking.PaidAmount = 0m;
        booking.RefundableAmount = 0m;
        _bookingService.AdminDetailAnswer = Result.Success(booking);
        var cut = RenderOpenDialog();

        ToggleRoom(cut, 0);

        cut.Find("#booking-cancel-refund-21").GetAttribute("value").ShouldBe("0");
        cut.Find("#booking-cancel-refund-21").HasAttribute("disabled").ShouldBeTrue();
        cut.Find(".booking-cancel-refund-hint").TextContent.ShouldContain("nothing to refund");
    }

    [Fact]
    public void UpdateTheTotalWhenARefundChanges()
    {
        var cut = RenderOpenDialog();
        SelectAllRooms(cut);

        cut.Find("#booking-cancel-refund-23").Change("100");

        cut.Find(".booking-cancel-summary-total").TextContent.ShouldBe("Total refund €250.00 of €720.00 refundable");
    }

    [Fact]
    public void RefuseRefundsAboveWhatCanStillBeRefunded()
    {
        var cut = RenderOpenDialog();
        SelectAllRooms(cut);
        cut.Find("#booking-cancel-reason").Change("Guest is ill");

        cut.Find("#booking-cancel-refund-23").Change("600");
        cut.Find(".booking-cancel-dialog form").Submit();

        cut.Find(".booking-cancel-refund-limit").TextContent.ShouldContain("€720.00");
        cut.Find(".booking-cancel-confirm").HasAttribute("disabled").ShouldBeTrue();
        _bookingService.CancelRequests.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveARoomFromTheSelectionWhenClickedAgain()
    {
        var cut = RenderOpenDialog();

        ToggleRoom(cut, 0);
        cut.FindAll(".booking-cancel-room.is-selected").Count.ShouldBe(1);
        ToggleRoom(cut, 0);

        cut.FindAll(".booking-cancel-room.is-selected").ShouldBeEmpty();
    }

    [Fact]
    public void SelectAndDeselectAllRooms()
    {
        var cut = RenderOpenDialog();

        SelectAllRooms(cut);
        cut.FindAll(".booking-cancel-room.is-selected").Count.ShouldBe(2);
        cut.Find(".booking-cancel-whole-note").TextContent.ShouldContain("cancels the whole booking");
        cut.Find(".booking-cancel-confirm").TextContent.Trim().ShouldBe("Cancel booking");

        SelectAllRooms(cut);
        cut.FindAll(".booking-cancel-room.is-selected").ShouldBeEmpty();
        cut.FindAll(".booking-cancel-whole-note").ShouldBeEmpty();
    }

    [Fact]
    public void NotCancelWithoutReason()
    {
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 0);

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldBeEmpty();
        cut.Find(".booking-cancel-dialog .help.is-danger").TextContent.ShouldBe("Please enter a reason for the cancellation.");
    }

    [Fact]
    public void NotCancelWithoutARoom()
    {
        var cut = RenderOpenDialog();
        cut.Find("#booking-cancel-reason").Change("Guest is ill");

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldBeEmpty();
        cut.Find(".booking-cancel-dialog .help.is-danger").TextContent.ShouldBe("Please select at least one room to cancel.");
    }

    [Fact]
    public void SendOnlyTheSelectedRoomWithItsRefund()
    {
        // the 2 rooms fit 2 guests each, so the 2 guests still fit when 1 room stays
        var booking = TestAdminDetails.Full();
        booking.Adults = 2;
        _bookingService.AdminDetailAnswer = Result.Success(booking);
        _bookingService.CancelAnswer = Result.Success(new BookingResponse.Cancel
        {
            BookingId = 7,
            BookingNumber = booking.BookingNumber,
            CancelledAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            RefundAmount = 100m,
            CancelledRoomCount = 1,
            RemainingRoomCount = 1,
        });
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 1);
        cut.Find("#booking-cancel-refund-23").Change("100");
        cut.Find("#booking-cancel-reason").Change("Guest is ill");
        cut.Find(".booking-cancel-confirm").TextContent.Trim().ShouldBe("Cancel 1 room");

        cut.Find(".booking-cancel-dialog form").Submit();

        var request = _bookingService.CancelRequests.ShouldHaveSingleItem();
        var room = request.Rooms.ShouldHaveSingleItem();
        room.BookingRoomId.ShouldBe(23);
        room.RefundAmount.ShouldBe(100m);
        request.Adults.ShouldBeNull();
        var success = cut.Find(".booking-detail-cancel-success").TextContent;
        success.ShouldContain("1 room of booking 20261004-SVM-000007 cancelled, 1 still booked");
        success.ShouldContain("A refund of €100.00 is recorded");
    }

    /// <summary>
    /// 3 guests, cancelling 1 of the 2 rooms leaves a room for 2, so the new nb is needed
    /// </summary>
    [Fact]
    public void AskForTheNewNbOfGuestsWhenTheyNoLongerFit()
    {
        var cut = RenderOpenDialog();
        cut.FindAll(".booking-cancel-guests").ShouldBeEmpty();

        ToggleRoom(cut, 0);

        cut.Find(".booking-cancel-guests-hint").TextContent.ShouldContain("has 3 guests");
        cut.Find(".booking-cancel-guests-hint").TextContent.ShouldContain("fit at most 2");
    }

    [Fact]
    public void NotCancelWithoutTheNewNbOfGuests()
    {
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 0);
        cut.Find("#booking-cancel-reason").Change("Guest is ill");

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldBeEmpty();
        cut.Find(".booking-cancel-error").TextContent.ShouldContain("remaining rooms fit at most 2");
    }

    [Fact]
    public void NotCancelWithTooManyGuestsForTheRemainingRooms()
    {
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 0);
        cut.Find("#booking-cancel-reason").Change("Guest is ill");
        cut.Find("#booking-cancel-adults").Change("3");

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldBeEmpty();
        cut.Find(".booking-cancel-error").TextContent.ShouldContain("remaining rooms fit at most 2");
    }

    [Fact]
    public void SendTheNewNbOfGuests()
    {
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 0);
        cut.Find("#booking-cancel-reason").Change("Guest is ill");
        cut.Find("#booking-cancel-adults").Change("2");

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldHaveSingleItem().Adults.ShouldBe(2);
    }

    /// <summary>
    /// the nb entered for 1 room must not be sent when the admin then selects all rooms, nobody stays
    /// </summary>
    [Fact]
    public void ForgetTheNewNbOfGuestsWhenNoRoomsRemain()
    {
        var cut = RenderOpenDialog();
        ToggleRoom(cut, 0);
        cut.Find("#booking-cancel-adults").Change("2");
        ToggleRoom(cut, 1);
        cut.FindAll(".booking-cancel-guests").ShouldBeEmpty();
        cut.Find("#booking-cancel-reason").Change("Guest is ill");

        cut.Find(".booking-cancel-dialog form").Submit();

        _bookingService.CancelRequests.ShouldHaveSingleItem().Adults.ShouldBeNull();
    }

    [Fact]
    public void ListOnlyTheActiveRoomsInTheDialog()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderOpenDialog();

        var room = cut.FindAll(".booking-cancel-room").ShouldHaveSingleItem();
        room.TextContent.ShouldContain("Room 101");
        cut.FindAll(".booking-cancel-room.is-selected").Count.ShouldBe(1);
        cut.Find(".booking-cancel-summary-total").TextContent.ShouldContain("of €510.00 refundable");
    }

    [Fact]
    public void ShowThePartlyCancelledBooking()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderPage();

        // the payment status stays visible next to the partial tag
        cut.Find(".booking-detail-status").TextContent.ShouldContain("Paid");
        cut.Find(".booking-detail-partial-tag").TextContent.ShouldBe("1 of 2 rooms cancelled");
        // only the cancelled room is listed in the cancellation card and marked in the stay
        var cancelled = cut.FindAll(".booking-detail-cancelled-room").ShouldHaveSingleItem();
        cancelled.TextContent.ShouldContain("Room 103");
        cancelled.QuerySelector(".booking-detail-refund")!.TextContent.ShouldBe("€210.00");
        cut.Find(".booking-detail-room.is-cancelled").TextContent.ShouldContain("103");
        cut.FindAll(".booking-detail-room-tag").Count.ShouldBe(1);
        cut.Find(".booking-detail-refunded-amount").TextContent.ShouldBe("€210.00");
        // the last room can still be cancelled
        cut.Find(".booking-detail-cancel-open").TextContent.Trim().ShouldBe("Cancel booking");
    }

    [Fact]
    public void ShowTheRemainingTotalOfAPartlyCancelledBooking()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderPage();

        cut.Find(".booking-detail-price-row.is-cancelled").TextContent.ShouldContain("Room 103");
        cut.Find(".booking-detail-original-total").TextContent.ShouldBe("€720.00");
        cut.Find(".booking-detail-price-refunds").TextContent.ShouldBe("€210.00");
        cut.Find(".booking-detail-total").TextContent.ShouldContain("Remaining total");
        cut.Find(".booking-detail-total-amount").TextContent.ShouldBe("€300.00");
    }

    /// <summary>
    /// the cancelled room shows the entitlement of its own date, the active room the one of today
    /// </summary>
    [Fact]
    public void ShowTheRefundOfTheOwnDateOfEachRoom()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderPage();

        var entitlements = cut.FindAll(".booking-detail-entitlement");
        entitlements[0].TextContent.ShouldContain("Refund if cancelled today");
        entitlements[1].TextContent.ShouldContain("Entitlement on 05 Oct 2026");
    }

    [Fact]
    public void ShowNoPartialTagOrCancellationForAnActiveBooking()
    {
        var cut = RenderPage();

        cut.FindAll(".booking-detail-partial-tag").ShouldBeEmpty();
        cut.FindAll(".booking-detail-cancellation").ShouldBeEmpty();
        cut.FindAll(".booking-detail-original-total").ShouldBeEmpty();
        cut.Find(".booking-detail-total").TextContent.ShouldNotContain("Remaining");
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
        var cut = RenderOpenDialog();
        SelectAllRooms(cut);
        cut.Find("#booking-cancel-reason").Change("Guest is ill");
        // the page reloads the booking after cancelling
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        cut.Find(".booking-cancel-dialog form").Submit();

        var request = _bookingService.CancelRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.Reason.ShouldBe("Guest is ill");
        request.Rooms.Select(x => (x.BookingRoomId, x.RefundAmount)).ShouldBe([(21, 150m), (23, 210m)]);
        request.Adults.ShouldBeNull();
        cut.FindAll(".booking-cancel-dialog").ShouldBeEmpty();
        var success = cut.Find(".booking-detail-cancel-success").TextContent;
        success.ShouldContain("Booking 20261004-SVM-000007 is cancelled");
        success.ShouldContain("A refund of €360.00 is recorded");
        cut.Find(".booking-detail-status .tag.is-danger").TextContent.Trim().ShouldBe("Cancelled");
    }

    [Fact]
    public void ShowWhyTheServerRefusedTheCancellation()
    {
        _bookingService.CancelAnswer = Result.Conflict("Booking 20261004-SVM-000007 is already cancelled.");
        var cut = RenderOpenDialog();
        SelectAllRooms(cut);
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
        cut.Find(".booking-detail-original-total").TextContent.ShouldBe("€720.00");
        cut.Find(".booking-detail-total-amount").TextContent.ShouldBe("€0.00");
        cut.FindAll(".booking-detail-partial-tag").ShouldBeEmpty();
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
        booking.Payments.Single().PaidAt = booking.Rooms[0].Cancellation!.CancelledAt.AddHours(1);
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.Find(".booking-detail-paid-after-cancel").TextContent.ShouldContain("for €720.00 more than the remaining rooms cost");
    }

    /// <summary>
    /// an open checkout for the old amount can be paid after 1 room is cancelled, the admin has to check the amount
    /// </summary>
    [Fact]
    public void WarnAboutAPaymentAfterTheCancellationOfOneRoom()
    {
        var booking = TestAdminDetails.PartlyCancelled();
        booking.Payments.Single().PaidAt = booking.Rooms[1].Cancellation!.CancelledAt.AddHours(1);
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        // 720 paid, only room 101 of 300 is left
        cut.Find(".booking-detail-paid-after-cancel").TextContent.ShouldContain("€420.00 more");
    }

    /// <summary>
    /// a checkout created after the cancellation only charges the remaining rooms, that payment is correct
    /// </summary>
    [Fact]
    public void NotWarnAboutTheRightAmountPaidAfterTheCancellation()
    {
        var booking = TestAdminDetails.PartlyCancelled();
        var payment = booking.Payments.Single();
        payment.PaidAt = booking.Rooms[1].Cancellation!.CancelledAt.AddHours(1);
        payment.Amount = 300m;
        booking.PaidAmount = 300m;
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.FindAll(".booking-detail-paid-after-cancel").ShouldBeEmpty();
    }

    /// <summary>
    /// paid in full and cancelled later without a full refund, the guest paid for rooms that were still booked
    /// </summary>
    [Fact]
    public void NotWarnWhenARoomIsCancelledAfterThePayment()
    {
        var booking = TestAdminDetails.PartlyCancelled();
        booking.Payments.Single().PaidAt = booking.Rooms[1].Cancellation!.CancelledAt.AddHours(-1);
        _bookingService.AdminDetailAnswer = Result.Success(booking);

        var cut = RenderPage();

        cut.FindAll(".booking-detail-paid-after-cancel").ShouldBeEmpty();
    }

    [Fact]
    public void NotWarnAboutAPaymentBeforeTheCancellation()
    {
        // paid on 4 Oct, cancelled on 10 Oct with a partial refund
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(360m));

        var cut = RenderPage();

        cut.FindAll(".booking-detail-paid-after-cancel").ShouldBeEmpty();
    }

    // ── refund payments ─────────────────────────────────────────────────────

    [Fact]
    public void ShowTheBookingAndTheRefundStatusSideBySide()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderPage();

        var status = cut.Find(".booking-detail-status");
        status.QuerySelector(".tag.is-primary")!.TextContent.Trim().ShouldBe("Paid");
        status.QuerySelector(".refund-tag-pending")!.TextContent.Trim().ShouldBe("Refund pending");
    }

    [Fact]
    public void ShowNoRefundStatusWithoutCancelledRooms()
    {
        var cut = RenderPage();

        cut.FindAll(".refund-tag").ShouldBeEmpty();
    }

    [Fact]
    public void OfferToMarkAnOpenRefundAsPaid()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());

        var cut = RenderPage();

        cut.Find(".booking-detail-refund-payment.is-open").TextContent.ShouldContain("Not paid back yet");
        cut.Find(".booking-detail-refund-mark").TextContent.Trim().ShouldBe("Mark refund as paid");
        cut.Find(".booking-detail-price-refunds-open").TextContent.ShouldBe("€210.00");
        cut.Find(".booking-detail-cancellation .booking-detail-refund-note").TextContent.ShouldContain("€210.00 still has to be paid back");
    }

    [Fact]
    public void NotOfferARefundPaymentWithoutRefund()
    {
        _bookingService.AdminDetailAnswer = Result.Success(Cancelled(0m));

        var cut = RenderPage();

        cut.FindAll(".booking-detail-refund-payment").ShouldBeEmpty();
        cut.Find(".booking-detail-status .refund-tag-none").TextContent.Trim().ShouldBe("No refund");
    }

    [Fact]
    public void PrefillTheRefundFormWithTodayAndTheDecidedRefund()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());
        var cut = RenderPage();

        cut.Find(".booking-detail-refund-mark").Click();

        cut.Find("#booking-refund-paid-on").GetAttribute("value").ShouldBe(DateTime.Today.ToString("yyyy-MM-dd"));
        cut.Find("#booking-refund-amount").GetAttribute("value").ShouldBe("210");
        cut.FindAll(".booking-refund-amount-hint").ShouldBeEmpty();
    }

    [Fact]
    public void RecordTheRefundPaymentWithAnotherAmountAndANote()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-mark").Click();

        cut.Find("#booking-refund-amount").Change("200");
        cut.Find("#booking-refund-note").Change("Bank transfer");
        cut.Find(".booking-refund-amount-hint").TextContent.ShouldBe("The decided refund was €210.00.");
        cut.Find(".booking-detail-refund-payment form").Submit();

        var request = _bookingService.RecordRefundRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.BookingRoomId.ShouldBe(23);
        request.Amount.ShouldBe(200m);
        request.Note.ShouldBe("Bank transfer");
        request.PaidOn.ShouldBe(DateOnly.FromDateTime(DateTime.Today));
        cut.Find(".booking-detail-cancel-success").TextContent.ShouldContain("The refund of €200.00 for room 103 is recorded as paid.");
        cut.FindAll(".booking-detail-refund-payment form").ShouldBeEmpty();
    }

    [Fact]
    public void NotRecordARefundPaymentWithoutAmount()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-mark").Click();

        cut.Find("#booking-refund-amount").Change("0");
        cut.Find(".booking-detail-refund-payment form").Submit();

        _bookingService.RecordRefundRequests.ShouldBeEmpty();
        cut.Find(".booking-detail-refund-payment .help.is-danger").TextContent.ShouldBe("Please enter the amount that was paid back.");
    }

    [Fact]
    public void ShowWhyTheRefundPaymentWasRefused()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());
        _bookingService.RefundAnswer = Result.Conflict("The refund of room 103 is already paid.");
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-mark").Click();

        cut.Find(".booking-detail-refund-payment form").Submit();

        cut.Find(".booking-refund-error").TextContent.ShouldBe("The refund of room 103 is already paid.");
        cut.FindAll(".booking-detail-refund-payment form").Count.ShouldBe(1);
    }

    [Fact]
    public void CloseTheRefundFormWithoutSaving()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelled());
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-mark").Click();

        cut.FindAll(".booking-refund-actions button")[0].Click();

        cut.FindAll(".booking-detail-refund-payment form").ShouldBeEmpty();
        _bookingService.RecordRefundRequests.ShouldBeEmpty();
    }

    [Fact]
    public void ShowThePaidRefund()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelledAndRefunded());

        var cut = RenderPage();

        cut.Find(".booking-detail-refund-paid-amount").TextContent.ShouldBe("€200.00");
        cut.Find(".booking-detail-refund-paid-on").TextContent.ShouldBe("08 Oct 2026");
        cut.Find(".booking-detail-refund-paid-note").TextContent.ShouldBe("Bank transfer");
        cut.Find(".booking-detail-status .refund-tag-refunded").TextContent.Trim().ShouldBe("Refunded");
        cut.Find(".booking-detail-price-refunds-paid").TextContent.ShouldBe("€200.00");
        cut.Find(".booking-detail-price-refunds-open").TextContent.ShouldBe("€0.00");
        cut.FindAll(".booking-detail-refund-mark").ShouldBeEmpty();
        cut.FindAll(".booking-detail-cancellation .booking-detail-refund-note").ShouldBeEmpty();
    }

    [Fact]
    public void AskBeforeUndoingARefundPayment()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelledAndRefunded());
        var cut = RenderPage();

        cut.Find(".booking-detail-refund-undo").Click();

        cut.Find(".booking-detail-refund-undo-confirm").TextContent.ShouldContain("Mark this refund as open again?");
        _bookingService.UndoRefundRequests.ShouldBeEmpty();
    }

    [Fact]
    public void UndoTheRefundPaymentOnceConfirmed()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelledAndRefunded());
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-undo").Click();

        cut.Find(".booking-detail-refund-undo-confirm-button").Click();

        var request = _bookingService.UndoRefundRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.BookingRoomId.ShouldBe(23);
        cut.Find(".booking-detail-cancel-success").TextContent.ShouldContain("The refund of room 103 is open again.");
    }

    [Fact]
    public void ShowWhyUndoingTheRefundPaymentFailed()
    {
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.PartlyCancelledAndRefunded());
        _bookingService.RefundAnswer = Result.Conflict("The refund of room 103 is not paid.");
        var cut = RenderPage();
        cut.Find(".booking-detail-refund-undo").Click();

        cut.Find(".booking-detail-refund-undo-confirm-button").Click();

        cut.Find(".booking-refund-error").TextContent.ShouldBe("The refund of room 103 is not paid.");
    }
}
