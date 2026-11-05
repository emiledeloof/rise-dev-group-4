using Ardalis.Result;
using Rise.Shared.Billing;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Billing;

/// <summary>tests the settle and print buttons of the bill page</summary>
public class BillSettleShould : BunitContext
{
    private readonly FakeBillingService _billingService = new();

    public BillSettleShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBillingService>(_billingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    private IRenderedComponent<Bill> RenderPage()
        => Render<Bill>(p => p.Add(x => x.BookingId, 7));

    [Fact]
    public void AskBeforeSettlingWithTheAmountToCollect()
    {
        var cut = RenderPage();

        cut.Find(".bill-settle").Click();

        cut.Find(".bill-settle-confirm").TextContent.ShouldContain("Collect €146.00 on site");
        _billingService.SettleRequests.ShouldBeEmpty();
    }

    [Fact]
    public void SettleAndShowTheClosedBill()
    {
        _billingService.ChangeAnswer = Result.Success(TestBills.Settled());
        var cut = RenderPage();

        cut.Find(".bill-settle").Click();
        cut.FindAll(".bill-settle-buttons button").Last().Click();

        _billingService.SettleRequests.ShouldHaveSingleItem().BookingId.ShouldBe(7);
        cut.Find(".bill-settled-banner").TextContent.ShouldContain("this bill is closed");
        cut.FindAll(".bill-settle").ShouldBeEmpty();
        cut.FindAll(".bill-line-actions").ShouldBeEmpty();
    }

    [Fact]
    public void ShowWhyTheServerRefusedTheSettlement()
    {
        _billingService.ChangeAnswer = Result.Conflict("Booking 20261004-SVM-000007 is already settled.");
        var cut = RenderPage();

        cut.Find(".bill-settle").Click();
        cut.FindAll(".bill-settle-buttons button").Last().Click();

        cut.Find(".bill-settle-error").TextContent.ShouldBe("Booking 20261004-SVM-000007 is already settled.");
    }

    [Fact]
    public void OpenThePrintDialog()
    {
        var print = JSInterop.SetupVoid("print");
        var cut = RenderPage();

        cut.Find(".bill-print").Click();

        print.VerifyInvoke("print");
    }

    [Fact]
    public void ShowNoSettleOrPrintForACancelledBooking()
    {
        _billingService.BillAnswer = Result.Success(TestBills.Cancelled());

        var cut = RenderPage();

        cut.FindAll(".bill-actions").ShouldBeEmpty();
    }
}
