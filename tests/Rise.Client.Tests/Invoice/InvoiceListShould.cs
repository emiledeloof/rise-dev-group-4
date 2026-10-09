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
    public void ShowStripePaymentInfoForPaidInvoice()
    {
        var cut = Render<InvoiceList>();
        var firstRow = cut.FindAll("ul li")[0];
        firstRow.TextContent.ShouldContain("Visa •••• 4242");
        firstRow.TextContent.ShouldContain("via Stripe");
        firstRow.QuerySelectorAll("a").Select(a => a.GetAttribute("href"))
            .ShouldBe(["https://pay.stripe.com/receipts/test_1", "https://dashboard.stripe.com/test/payments/pi_test_1"]);
    }

    [Fact]
    public void HideStripePaymentInfoForOpenInvoice()
    {
        var cut = Render<InvoiceList>();
        var openRow = cut.FindAll("ul li")[1];
        openRow.TextContent.ShouldNotContain("Stripe");
        openRow.QuerySelectorAll("a").ShouldBeEmpty();
    }
}
