using Rise.Client.Pages;
using Rise.Shared.Identity;
using Rise.Shared.Invoice;
using Xunit.Abstractions;
using Shouldly;

namespace Rise.Client.Invoice;

public class InvoiceListShould : BunitContext
{
    public InvoiceListShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddScoped<IInvoiceService, FakeInvoiceService>();
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void ShowAllInvoices()
    {
        var cut = Render<InvoiceList>();
        cut.FindAll("ul li").Count.ShouldBe(3);
    }

    [Fact]
    public void ShowBookingNumberAndGuestName()
    {
        var cut = Render<InvoiceList>();
        var firstRow = cut.FindAll("ul li")[0].TextContent;
        firstRow.ShouldContain("20261001-0001");
        firstRow.ShouldContain("Guest 1");
        firstRow.ShouldContain("€100.00");
        firstRow.ShouldContain("Paid");
    }

    [Fact]
    public void ShowNoCancellationForAnInvoiceWithoutCancelledRooms()
    {
        var cut = Render<InvoiceList>();

        cut.FindAll("li.invoice-row")[0].QuerySelector(".invoice-cancellation").ShouldBeNull();
    }

    [Fact]
    public void ShowTheCancelledRoomsAndRefundOfAPartlyCancelledBooking()
    {
        var cut = Render<InvoiceList>();

        var row = cut.FindAll("li.invoice-row")[1];
        var cancellation = row.QuerySelector(".invoice-cancellation")!.TextContent;
        cancellation.ShouldContain("1 of 2 rooms cancelled");
        cancellation.ShouldContain("€150.00 refunded on this booking");
        // the row keeps the amount that was paid
        row.QuerySelector(".invoice-amount")!.TextContent.ShouldContain("€200.00");
    }

    [Fact]
    public void ShowAFullyCancelledBookingWithoutARefund()
    {
        var cut = Render<InvoiceList>();

        var row = cut.FindAll("li.invoice-row")[2];
        row.QuerySelector(".invoice-cancellation")!.TextContent.ShouldContain("Booking cancelled");
        row.QuerySelector(".invoice-refund").ShouldBeNull();
    }

    [Fact]
    public void ShowStripePaymentInfoForPaidInvoice()
    {
        var cut = Render<InvoiceList>();
        var firstRow = cut.FindAll("ul li")[0];
        firstRow.TextContent.ShouldContain("Visa •••• 4242");
        firstRow.TextContent.ShouldContain("via Stripe");
        firstRow.QuerySelectorAll(".invoice-stripe a").Select(a => a.GetAttribute("href"))
            .ShouldBe(["https://pay.stripe.com/receipts/test_1", "https://dashboard.stripe.com/test/payments/pi_test_1"]);
    }

    [Fact]
    public void HideStripePaymentInfoForOpenInvoice()
    {
        var cut = Render<InvoiceList>();
        var openRow = cut.FindAll("ul li")[1];
        openRow.TextContent.ShouldNotContain("Stripe");
        openRow.QuerySelectorAll(".invoice-stripe a").ShouldBeEmpty();
    }

    [Fact]
    public void LinkTheInvoiceToTheFinalBill()
    {
        var cut = Render<InvoiceList>();

        cut.FindAll("a.invoice-booking-link")[1].GetAttribute("href").ShouldBe("admin/bookings/2/bill?from=invoices");
    }

    [Fact]
    public void ShowWhenTheBillIsSettled()
    {
        var cut = Render<InvoiceList>();

        var rows = cut.FindAll("ul li");
        rows[0].QuerySelector(".invoice-settled")!.TextContent.ShouldBe("Bill settled");
        rows[1].QuerySelector(".invoice-settled").ShouldBeNull();
    }

    [Fact]
    public void ShowAViewBillCueOnEveryRow()
    {
        var cut = Render<InvoiceList>();

        cut.FindAll(".invoice-view-bill").Count.ShouldBe(3);
    }
}
