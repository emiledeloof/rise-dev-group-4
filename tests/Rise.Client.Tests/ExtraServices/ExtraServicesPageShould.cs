using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Client.ExtraServices;
using Rise.Shared.ExtraServices;
using Rise.Shared.Identity;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Client.Tests.ExtraServices;

public class ExtraServicesPageShould : BunitContext
{
    private readonly FakeExtraServiceService _service = new();

    public ExtraServicesPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IExtraServiceService>(_service);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void DisplayExtraServicesList()
    {
        var cut = Render<Rise.Client.ExtraServices.ExtraServices>();

        var rows = cut.FindAll(".service-table tbody tr");
        rows.Count.ShouldBe(2);

        rows[0].TextContent.ShouldContain("Breakfast");
        rows[0].TextContent.ShouldContain("Morning buffet");
        rows[0].TextContent.ShouldContain("per person");
        rows[0].TextContent.ShouldContain("Active");

        rows[1].TextContent.ShouldContain("Extra Cleaning");
        rows[1].TextContent.ShouldContain("per booking");
        rows[1].TextContent.ShouldContain("Inactive");
    }

    [Fact]
    public void OpenCreateFormWhenButtonClicked()
    {
        var cut = Render<Rise.Client.ExtraServices.ExtraServices>();

        cut.Find(".service-heading button").Click();

        cut.Find(".service-panel").ShouldNotBeNull();
        cut.Find("#service-form-heading").TextContent.ShouldBe("New extra service");
    }

    [Fact]
    public async Task ToggleServiceActiveStatus()
    {
        var cut = Render<Rise.Client.ExtraServices.ExtraServices>();

        var toggle = cut.Find("input[role='switch'][aria-label='Deactivate Breakfast']");
        await cut.InvokeAsync(() => toggle.Change(false));

        _service.Services.First(s => s.Name == "Breakfast").IsActive.ShouldBeFalse();
    }

    [Fact]
    public void OpenEditFormWhenEditButtonClicked()
    {
        var cut = Render<Rise.Client.ExtraServices.ExtraServices>();

        cut.FindAll(".rate-edit-btn")[0].Click();

        cut.Find(".service-panel").ShouldNotBeNull();
        cut.Find("#service-form-heading").TextContent.ShouldBe("Edit extra service");
        cut.Find("input[role='switch'][aria-label='Active (available for bookings and rates)']").ShouldNotBeNull();
    }
}
