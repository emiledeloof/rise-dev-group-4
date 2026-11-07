using Microsoft.AspNetCore.Components;
using Rise.Shared.Billing;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Billing;

/// <summary>tests where the back link of the bill page goes</summary>
public class BillBackLinkShould : BunitContext
{
    public BillBackLinkShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBillingService>(new FakeBillingService());
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void GoBackToTheBookingByDefault()
    {
        var cut = Render<Bill>(p => p.Add(x => x.BookingId, 7));

        cut.Find(".bill-back").GetAttribute("href").ShouldBe("admin/bookings/7");
        cut.Find(".bill-back").TextContent.Trim().ShouldBe("Booking");
    }

    [Fact]
    public void GoBackToTheInvoicesWhenOpenedFromThere()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("admin/bookings/7/bill?from=invoices");

        var cut = Render<Bill>(p => p.Add(x => x.BookingId, 7));

        cut.Find(".bill-back").GetAttribute("href").ShouldBe("admin/invoice-list");
        cut.Find(".bill-back").TextContent.Trim().ShouldBe("Invoices");
    }
}
