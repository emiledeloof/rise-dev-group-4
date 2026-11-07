using Ardalis.Result;
using Rise.Shared.Billing;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Billing;

/// <summary>
/// Tests for the admin final bill page, see Billing/Bill.razor.
/// </summary>
public class BillShould : BunitContext
{
    private readonly FakeBillingService _billingService = new();

    public BillShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBillingService>(_billingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    private IRenderedComponent<Bill> RenderPage()
        => Render<Bill>(p => p.Add(x => x.BookingId, 7));

    [Fact]
    public void ShowTheAccommodationAndServices()
    {
        var cut = RenderPage();

        cut.Find(".bill-number").TextContent.ShouldBe("20261004-SVM-000007");
        cut.Find(".bill-accommodation .bill-line").TextContent.ShouldContain("Room 101");
        cut.Find(".bill-accommodation .bill-line-amount").TextContent.ShouldBe("€330.00");
        cut.Find(".bill-other-service-description").TextContent.ShouldBe("Minibar");
        cut.Find(".bill-other-service .bill-line-amount").TextContent.ShouldBe("€16.00");
    }

    [Fact]
    public void ShowTheFreeLinesAsOtherServicesBelowTheExtraServices()
    {
        var cut = RenderPage();

        var cards = cut.FindAll(".bill-main > section").Select(x => x.ClassList).ToList();
        cards.FindIndex(x => x.Contains("bill-extra-services"))
            .ShouldBeLessThan(cards.FindIndex(x => x.Contains("bill-other-services")));
        cut.Find(".bill-extra-services").TextContent.ShouldContain("No extra services booked.");
        cut.Find(".bill-extra-services").TextContent.ShouldNotContain("Minibar");
        cut.Find(".bill-other-services .eyebrow").TextContent.ShouldBe("Other services");
        cut.Find(".bill-other-services").TextContent.ShouldContain("Minibar");
    }

    [Fact]
    public void ShowTheTotalVatAndOutstandingBalance()
    {
        var cut = RenderPage();

        cut.Find(".bill-total-amount").TextContent.ShouldBe("€346.00");
        var vatRows = cut.FindAll(".bill-vat-row");
        vatRows.Count.ShouldBe(2);
        vatRows[0].TextContent.ShouldContain("10 %");
        vatRows[0].TextContent.ShouldContain("€30.00");
        cut.Find(".bill-net-amount").TextContent.ShouldBe("€313.11");
        cut.Find(".bill-paid-amount").TextContent.ShouldContain("€200.00");
        cut.Find(".bill-balance.is-open .bill-balance-label").TextContent.ShouldBe("Outstanding");
        cut.Find(".bill-balance-amount").TextContent.ShouldBe("€146.00");
    }

    [Fact]
    public void HideZeroVatGroups()
    {
        var bill = TestBills.Open();
        bill.VatGroups = [new BillingDto.VatGroup { VatPercentage = 0m, GrossAmount = 346m, NetAmount = 346m, VatAmount = 0m }];
        _billingService.BillAnswer = Result.Success(bill);

        var cut = RenderPage();

        cut.FindAll(".bill-vat-row").ShouldBeEmpty();
    }

    [Fact]
    public void MarkOpenPaymentsAsNotCounted()
    {
        var cut = RenderPage();

        var payments = cut.FindAll(".bill-payment");
        payments.Count.ShouldBe(2);
        payments[1].ClassList.ShouldContain("is-open");
        payments[1].TextContent.ShouldContain("Open");
    }

    [Fact]
    public void ShowACreditWhenOverpaid()
    {
        var bill = TestBills.Open();
        bill.OutstandingBalance = -20m;
        _billingService.BillAnswer = Result.Success(bill);

        var cut = RenderPage();

        cut.Find(".bill-balance.is-credit .bill-balance-label").TextContent.ShouldBe("Credit");
        cut.Find(".bill-balance-amount").TextContent.ShouldBe("€20.00");
    }

    [Fact]
    public void ShowNothingDueAndNoControlsForACancelledBooking()
    {
        _billingService.BillAnswer = Result.Success(TestBills.Cancelled());

        var cut = RenderPage();

        cut.Find(".bill-cancelled-banner").TextContent.ShouldContain("nothing due");
        cut.Find(".bill-refund-amount").TextContent.ShouldBe("+ €150.00");
        cut.FindAll(".bill-add-line").ShouldBeEmpty();
        cut.FindAll(".bill-edit-line").ShouldBeEmpty();
        cut.FindAll(".bill-delete-line").ShouldBeEmpty();
    }

    [Fact]
    public void ShowAPartlyCancelledBookingWithItsControls()
    {
        _billingService.BillAnswer = Result.Success(TestBills.PartlyCancelled());

        var cut = RenderPage();

        cut.Find(".bill-cancelled-banner.is-partial").TextContent.ShouldContain("1 of 2 rooms cancelled");
        cut.Find(".bill-cancelled-banner").TextContent.ShouldNotContain("nothing due");
        cut.FindAll(".bill-add-line").Count.ShouldBe(1);
        cut.FindAll(".bill-edit-line").Count.ShouldBe(1);
        cut.FindAll(".bill-delete-line").Count.ShouldBe(1);
    }

    [Fact]
    public void MarkTheCancelledRoomAndShowItsFee()
    {
        _billingService.BillAnswer = Result.Success(TestBills.PartlyCancelled());

        var cut = RenderPage();

        var cancelled = cut.Find(".bill-room.is-cancelled");
        cancelled.TextContent.ShouldContain("Room 102");
        cancelled.TextContent.ShouldContain("Cancelled");
        cancelled.QuerySelector(".bill-room-cancellation")!.TextContent.ShouldContain("Guest is ill");
        cancelled.QuerySelector(".bill-line-amount")!.TextContent.ShouldBe("€150.00");
        cut.FindAll(".bill-room").Count.ShouldBe(2);
        cut.Find(".bill-fee-amount").TextContent.ShouldBe("€50.00");
    }

    [Fact]
    public void ShowTheFeesAndRefundsInTheSummary()
    {
        _billingService.BillAnswer = Result.Success(TestBills.PartlyCancelled());

        var cut = RenderPage();

        cut.Find(".bill-accommodation-total").TextContent.ShouldBe("€330.00");
        cut.Find(".bill-fees-total").TextContent.ShouldBe("€50.00");
        cut.Find(".bill-total-amount").TextContent.ShouldBe("€396.00");
        cut.Find(".bill-refunded-amount").TextContent.ShouldBe("+ €100.00");
        cut.Find(".bill-balance.is-settled .bill-balance-label").TextContent.ShouldBe("Settled");
    }

    [Fact]
    public void ShowARefundRowPerCancelledRoom()
    {
        _billingService.BillAnswer = Result.Success(TestBills.PartlyCancelled());

        var cut = RenderPage();

        var refund = cut.FindAll(".bill-refund").ShouldHaveSingleItem();
        refund.TextContent.ShouldContain("Room 102");
        refund.QuerySelector(".bill-refund-amount")!.TextContent.ShouldBe("+ €100.00");
    }

    [Fact]
    public void HideTheFeeAndRefundRowsWithoutACancellation()
    {
        var cut = RenderPage();

        cut.FindAll(".bill-cancelled-banner").ShouldBeEmpty();
        cut.FindAll(".bill-room.is-cancelled").ShouldBeEmpty();
        cut.FindAll(".bill-fee").ShouldBeEmpty();
        cut.FindAll(".bill-fees-row").ShouldBeEmpty();
        cut.FindAll(".bill-refund").ShouldBeEmpty();
        cut.FindAll(".bill-refunded-row").ShouldBeEmpty();
    }

    [Fact]
    public void ShowAMessageForAnUnknownBooking()
    {
        _billingService.BillAnswer = Result.NotFound();

        var cut = RenderPage();

        cut.Find(".bill-error").TextContent.ShouldBe("This booking could not be found.");
    }

    [Fact]
    public void AddALineAndShowTheNewTotals()
    {
        var cut = RenderPage();

        cut.Find(".bill-add-line").Click();
        cut.Find("#bill-line-description").Change("Guided vineyard tour");
        cut.Find("#bill-line-quantity").Change("1");
        cut.Find("#bill-line-price").Change("45");
        cut.Find(".bill-line-dialog form").Submit();

        var request = _billingService.AddRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.Description.ShouldBe("Guided vineyard tour");
        request.UnitPrice.ShouldBe(45m);
        request.VatPercentage.ShouldBe(VatRates.Default);
        cut.FindAll(".bill-line-dialog").ShouldBeEmpty();
        cut.Find(".bill-total-amount").TextContent.ShouldBe("€391.00");
    }

    [Fact]
    public void NotAddALineWithoutDescription()
    {
        var cut = RenderPage();

        cut.Find(".bill-add-line").Click();
        cut.Find("#bill-line-price").Change("45");
        cut.Find(".bill-line-dialog form").Submit();

        _billingService.AddRequests.ShouldBeEmpty();
        cut.Find(".bill-line-dialog .help.is-danger").TextContent.ShouldBe("Please describe what the guest used.");
    }

    [Fact]
    public void PrefillTheSheetWhenEditingALine()
    {
        var cut = RenderPage();

        cut.Find(".bill-edit-line").Click();

        cut.Find("#bill-line-description").GetAttribute("value").ShouldBe("Minibar");
        cut.Find("#bill-line-quantity").GetAttribute("value").ShouldBe("2");
        cut.Find("#bill-line-vat").GetAttribute("value").ShouldBe("22");

        cut.Find(".bill-line-dialog form").Submit();

        var request = _billingService.EditRequests.ShouldHaveSingleItem();
        request.LineId.ShouldBe(11);
        request.VatPercentage.ShouldBe(22m);
    }

    [Fact]
    public void AskBeforeDeletingALine()
    {
        var cut = RenderPage();

        cut.Find(".bill-delete-line").Click();

        _billingService.DeleteRequests.ShouldBeEmpty();
        cut.Find(".bill-delete-confirm").TextContent.ShouldContain("Minibar");

        cut.Find(".bill-delete-yes").Click();

        var request = _billingService.DeleteRequests.ShouldHaveSingleItem();
        request.LineId.ShouldBe(11);
        cut.FindAll(".bill-delete-confirm").ShouldBeEmpty();
    }

    [Fact]
    public void ShowWhyTheServerRefusedALine()
    {
        _billingService.ChangeAnswer = Result.Conflict("Booking 20261004-SVM-000007 is cancelled, its bill can't be changed.");
        var cut = RenderPage();

        cut.Find(".bill-edit-line").Click();
        cut.Find(".bill-line-dialog form").Submit();

        cut.Find(".bill-line-error").TextContent.ShouldContain("is cancelled");
        cut.FindAll(".bill-line-dialog").Count.ShouldBe(1);
    }

    [Fact]
    public void RecordACashPaymentAndShowTheNewBalance()
    {
        _billingService.ChangeAnswer = Result.Success(TestBills.WithCashPayment());
        var cut = RenderPage();

        cut.Find(".bill-record-payment").Click();
        cut.Find("#bill-payment-amount").Change("16");
        cut.Find("#bill-payment-reference").Change("Receipt 0042");
        cut.Find(".bill-payment-dialog form").Submit();

        var request = _billingService.RecordPaymentRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.Amount.ShouldBe(16m);
        request.Method.ShouldBe(PaymentMethods.Cash);
        request.Reference.ShouldBe("Receipt 0042");
        cut.FindAll(".bill-payment-dialog").ShouldBeEmpty();
        cut.Find(".bill-balance-amount").TextContent.ShouldBe("€130.00");
        cut.FindAll(".bill-payment-method")[2].TextContent.ShouldContain("Cash");
        cut.Find(".bill-payment-reference").TextContent.ShouldBe("Receipt 0042");
    }

    [Fact]
    public void PrefillTheOutstandingBalanceWhenRecordingAPayment()
    {
        var cut = RenderPage();

        cut.Find(".bill-record-payment").Click();

        cut.Find("#bill-payment-amount").GetAttribute("value").ShouldBe("146");
        cut.Find(".bill-balance-after").TextContent.ShouldBe("€0.00");
        cut.Find(".bill-method.is-selected").TextContent.ShouldContain("Cash");
    }

    [Fact]
    public void RecordTheChosenPaymentMethod()
    {
        var cut = RenderPage();

        cut.Find(".bill-record-payment").Click();
        cut.FindAll(".bill-method input")[1].Change(PaymentMethods.CardTerminal);
        cut.Find(".bill-payment-dialog form").Submit();

        _billingService.RecordPaymentRequests.ShouldHaveSingleItem().Method.ShouldBe(PaymentMethods.CardTerminal);
    }

    [Fact]
    public void NotRecordAPaymentWithoutAmount()
    {
        var cut = RenderPage();

        cut.Find(".bill-record-payment").Click();
        cut.Find("#bill-payment-amount").Change("0");
        cut.Find(".bill-payment-dialog form").Submit();

        _billingService.RecordPaymentRequests.ShouldBeEmpty();
        cut.Find(".bill-payment-dialog .help.is-danger").TextContent.ShouldBe("The amount must be more than €0.");
    }

    [Fact]
    public void HideRecordPaymentWhenNothingIsOutstanding()
    {
        var bill = TestBills.Open();
        bill.OutstandingBalance = 0m;
        _billingService.BillAnswer = Result.Success(bill);

        var cut = RenderPage();

        cut.FindAll(".bill-record-payment").ShouldBeEmpty();
    }

    [Fact]
    public void HideRecordPaymentForACancelledBooking()
    {
        _billingService.BillAnswer = Result.Success(TestBills.Cancelled());

        var cut = RenderPage();

        cut.FindAll(".bill-record-payment").ShouldBeEmpty();
    }

    [Fact]
    public void AskBeforeDeletingAnOnSitePayment()
    {
        _billingService.BillAnswer = Result.Success(TestBills.WithCashPayment());
        var cut = RenderPage();

        cut.Find(".bill-delete-payment").Click();

        _billingService.DeletePaymentRequests.ShouldBeEmpty();
        cut.Find(".bill-payments .bill-delete-confirm").TextContent.ShouldContain("€16.00");

        cut.Find(".bill-delete-payment-yes").Click();

        var request = _billingService.DeletePaymentRequests.ShouldHaveSingleItem();
        request.BookingId.ShouldBe(7);
        request.PaymentId.ShouldBe(3);
        cut.FindAll(".bill-delete-confirm").ShouldBeEmpty();
    }

    [Fact]
    public void NotOfferToDeleteAStripePayment()
    {
        _billingService.BillAnswer = Result.Success(TestBills.WithCashPayment());

        var cut = RenderPage();

        // only the cash payment, not the 2 Stripe ones
        cut.FindAll(".bill-delete-payment").Count.ShouldBe(1);
        cut.FindAll(".bill-payment-method")[0].TextContent.ShouldContain("Online · Stripe");
    }

    [Fact]
    public void ShowWhyTheServerRefusedAPayment()
    {
        _billingService.ChangeAnswer = Result.Invalid(new ValidationError("Amount", "The guest only owes 146.00 EUR."));
        var cut = RenderPage();

        cut.Find(".bill-record-payment").Click();
        cut.Find(".bill-payment-dialog form").Submit();

        cut.Find(".bill-payment-error").TextContent.ShouldContain("only owes");
        cut.FindAll(".bill-payment-dialog").Count.ShouldBe(1);
    }
}
