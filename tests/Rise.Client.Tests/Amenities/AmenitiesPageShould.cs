using Ardalis.Result;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rise.Shared.Amenities;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Amenities;

/// <summary>
/// tests for the admin amenities page, see Amenities/Amenities.razor
/// </summary>
public class AmenitiesPageShould : BunitContext
{
    private readonly FakeAmenityService _amenityService = new();

    public AmenitiesPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IAmenityService>(_amenityService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
        // the navigation lock calls js to ask the browser before a reload
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // opens the edit form of the wi-fi row
    private static void EditWifi(IRenderedComponent<Amenities> cut)
        => cut.FindAll("tr.amenity-row")[1].QuerySelectorAll("button")[0].Click();

    [Fact]
    public void ListEveryAmenityWithItsStatusAndRoomTypes()
    {
        var cut = Render<Amenities>();

        var rows = cut.FindAll("tr.amenity-row");
        rows.Count.ShouldBe(2);
        rows[0].ClassList.ShouldContain("is-inactive");
        rows[0].QuerySelector(".amenity-switch input")!.HasAttribute("checked").ShouldBeFalse();
        rows[1].QuerySelector(".amenity-switch input")!.HasAttribute("checked").ShouldBeTrue();
        rows[1].QuerySelector(".amenity-name")!.TextContent.ShouldContain("Wi-Fi");
        rows[1].QuerySelector(".amenity-translations")!.TextContent.ShouldContain("2 / 6");
        rows[1].QuerySelector(".amenity-room-types")!.TextContent.ShouldContain("Deluxe Room, Standard Room");
    }

    [Fact]
    public void CreateAnAmenityWithItsNamesAndIcon()
    {
        var cut = Render<Amenities>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New amenity")).Click();

        cut.Find("#amenity-name-en").Input("Hairdryer");
        cut.Find("#amenity-name-de").Input("Haartrockner");
        cut.Find("#amenity-icon").Change("fa-wind");
        cut.Find("form").Submit();

        _amenityService.LastCreate.ShouldNotBeNull();
        _amenityService.LastCreate.Names["en"].ShouldBe("Hairdryer");
        _amenityService.LastCreate.Names["de"].ShouldBe("Haartrockner");
        _amenityService.LastCreate.Icon.ShouldBe("fa-wind");
        cut.Find(".notification.is-success").TextContent.ShouldContain("Hairdryer");
    }

    [Fact]
    public void RequireTheEnglishName()
    {
        var cut = Render<Amenities>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New amenity")).Click();

        cut.Find("#amenity-icon").Change("fa-wind");
        cut.Find("form").Submit();

        _amenityService.LastCreate.ShouldBeNull();
        cut.Markup.ShouldContain("A name in English is required.");
    }

    [Fact]
    public void ShowTheMessageOfARefusedDuplicateName()
    {
        _amenityService.EditAnswer = Result.Conflict("An amenity named 'Wifi' already exists in Dutch.");
        var cut = Render<Amenities>();
        EditWifi(cut);

        cut.Find("form").Submit();

        cut.Find(".amenity-panel .notification.is-danger").TextContent.ShouldBe("An amenity named 'Wifi' already exists in Dutch.");
    }

    [Fact]
    public void DeactivateAnActiveAmenity()
    {
        var cut = Render<Amenities>();

        cut.FindAll("tr.amenity-row")[1].QuerySelector(".amenity-switch input")!.Change(false);

        _amenityService.StatusChanges.ShouldBe([(2, false)]);
        cut.Find(".notification.is-success").TextContent.ShouldContain("hidden from guests");
    }

    [Fact]
    public void ActivateAnInactiveAmenity()
    {
        var cut = Render<Amenities>();

        cut.FindAll("tr.amenity-row")[0].QuerySelector(".amenity-switch input")!.Change(true);

        _amenityService.StatusChanges.ShouldBe([(1, true)]);
    }

    [Fact]
    public void ShowTheSwitchAsActiveAgainWhenTheServerRefuses()
    {
        _amenityService.StatusAnswer = Result.NotFound("The amenity does not exist.");
        var cut = Render<Amenities>();

        cut.FindAll("tr.amenity-row")[1].QuerySelector(".amenity-switch input")!.Change(false);

        cut.Find(".notification.is-danger").TextContent.ShouldContain("does not exist");
        cut.FindAll("tr.amenity-row")[1].QuerySelector(".amenity-switch input")!.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void AskBeforeCancellingUnsavedChanges()
    {
        var cut = Render<Amenities>();
        EditWifi(cut);
        cut.Find("#amenity-name-en").Input("Free Wi-Fi");

        cut.Find(".amenity-cancel").Click();
        cut.Find(".unsaved-discard").Click();

        cut.FindAll(".amenity-panel").ShouldBeEmpty();
    }

    [Fact]
    public void KeepTheFormWhenTheAdminKeepsEditing()
    {
        var cut = Render<Amenities>();
        EditWifi(cut);
        cut.Find("#amenity-name-en").Input("Free Wi-Fi");
        cut.Find(".amenity-cancel").Click();

        cut.Find(".unsaved-keep").Click();

        cut.FindAll(".unsaved-dialog").ShouldBeEmpty();
        cut.Find("#amenity-name-en").GetAttribute("value").ShouldBe("Free Wi-Fi");
    }

    [Fact]
    public void CloseAnUnchangedFormWithoutAsking()
    {
        var cut = Render<Amenities>();
        EditWifi(cut);

        cut.Find(".amenity-cancel").Click();

        cut.FindAll(".unsaved-dialog").ShouldBeEmpty();
        cut.FindAll(".amenity-panel").ShouldBeEmpty();
    }

    [Fact]
    public void StopALinkOfTheAppWhileTheFormHasChanges()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Amenities>();
        EditWifi(cut);
        cut.Find("#amenity-name-en").Input("Free Wi-Fi");

        cut.InvokeAsync(() => navigation.NavigateTo("/admin/room-types"));

        cut.WaitForElement(".unsaved-dialog");
        navigation.Uri.ShouldNotEndWith("/admin/room-types");
    }
}
