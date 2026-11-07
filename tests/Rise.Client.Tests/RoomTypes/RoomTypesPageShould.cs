using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Client.Amenities;
using Rise.Shared.Amenities;
using Rise.Shared.Identity;
using Rise.Shared.RoomTypes;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.RoomTypes;

/// <summary>
/// tests for the characteristics of the admin room types page, see RoomTypes/RoomTypes.razor
/// </summary>
public class RoomTypesPageShould : BunitContext
{
    private readonly FakeRoomTypeService _roomTypeService = new();
    private readonly FakeAmenityService _amenityService = new();

    public RoomTypesPageShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IRoomTypeService>(_roomTypeService);
        Services.AddSingleton<IAmenityService>(_amenityService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
        // the navigation lock calls js to ask the browser before a reload
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<RoomTypes> RenderAndEditDeluxe()
    {
        var cut = Render<RoomTypes>();
        cut.Find(".room-type-edit").Click();
        return cut;
    }

    [Fact]
    public void ShowOnlyTheActiveAmenitiesAsCheckboxes()
    {
        var cut = RenderAndEditDeluxe();

        var amenities = cut.FindAll(".room-type-amenity");
        amenities.ShouldHaveSingleItem().TextContent.ShouldContain("Wi-Fi");
        amenities[0].QuerySelector("i")!.ClassList.ShouldContain("fa-wifi");
    }

    [Fact]
    public void TickTheAmenitiesOfTheRoomType()
    {
        var cut = RenderAndEditDeluxe();

        cut.Find(".room-type-amenity input").HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void MentionTheInactiveAmenitiesStillAssigned()
    {
        var cut = RenderAndEditDeluxe();

        cut.Find(".room-type-inactive").TextContent.ShouldContain("Bathtub");
    }

    [Fact]
    public void FillTheBedConfigurationPerLanguage()
    {
        var cut = RenderAndEditDeluxe();

        cut.Find("#room-type-bed-en").GetAttribute("value").ShouldBe("King bed");
        cut.Find("#room-type-bed-nl").GetAttribute("value").ShouldBe("Kingsize bed");
        cut.Find("#room-type-bed-fr").GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Fact]
    public void SaveTheUntickedAmenityAndTheNewBedConfiguration()
    {
        var cut = RenderAndEditDeluxe();

        cut.Find(".room-type-amenity input").Change(false);
        cut.Find("#room-type-bed-fr").Input("Lit king size");
        cut.Find("form").Submit();

        var saved = _roomTypeService.LastEdit.ShouldNotBeNull();
        // the inactive bathtub is still sent, the server keeps it anyway
        saved.AmenityIds.ShouldBe([1]);
        saved.BedConfigurations["fr"].ShouldBe("Lit king size");
        saved.BedConfigurations["en"].ShouldBe("King bed");
    }

    [Fact]
    public void LinkToTheAmenitiesPageWhenNoAmenityIsActive()
    {
        _amenityService.IndexAnswer = Ardalis.Result.Result.Success(new AmenityResponse.Index { Amenities = [] });

        var cut = RenderAndEditDeluxe();

        cut.Find(".room-type-no-amenities a").GetAttribute("href").ShouldBe("/admin/amenities");
    }

    [Fact]
    public void AskBeforeCancellingUnsavedCharacteristics()
    {
        var cut = RenderAndEditDeluxe();
        cut.Find(".room-type-amenity input").Change(false);

        cut.Find(".room-type-cancel").Click();

        cut.Find(".unsaved-dialog").ShouldNotBeNull();
    }

    [Fact]
    public void NotAskWhenAnAmenityIsTickedAgain()
    {
        var cut = RenderAndEditDeluxe();
        cut.Find(".room-type-amenity input").Change(false);
        cut.Find(".room-type-amenity input").Change(true);

        cut.Find(".room-type-cancel").Click();

        cut.FindAll(".unsaved-dialog").ShouldBeEmpty();
        cut.FindAll(".room-type-panel").ShouldBeEmpty();
    }
}
