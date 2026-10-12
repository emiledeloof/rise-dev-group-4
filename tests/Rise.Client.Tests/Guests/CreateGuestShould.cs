using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rise.Client.Bookings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Guests;

/// <summary>
/// Component tests for the guest details page (<see cref="Create"/>) using bUnit.
/// Verifies proper rendering of input fields, validation integration, and that the details go to the booking draft.
/// Follows naming convention &lt;Subject&gt;Should.
/// </summary>
public class CreateGuestShould : BunitContext
{
    public CreateGuestShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    // plugs the draft and opens the guest form
    private IRenderedComponent<Create> RenderWith(BookingDraft draft)
    {
        Services.AddSingleton(draft);
        Navigation.NavigateTo("guests/create");
        return Render<Create>();
    }

    // fills the required fields so client-side FluentValidation passes
    private static void FillValidForm(IRenderedComponent<Create> cut)
    {
        cut.Find("input[placeholder*='John']").Change("John");
        cut.Find("input[placeholder*='Doe']").Change("Doe");
        cut.Find("input[type='email']").Change("john.doe@example.com");
        cut.Find("input[placeholder*='123 456']").Change("+32123456789");
        cut.Find("input[placeholder*='Street']").Change("Kerkstraat 1");
        cut.Find("input[placeholder*='Belgium']").Change("Belgium");
    }

    [Fact]
    public void RenderRegistrationFormWithAllFields()
    {
        // Act: Render the component under test (cut)
        var cut = RenderWith(TestDrafts.WithRooms());

        // Assert: Verify title and subtitle
        cut.Find("h2.title").TextContent.ShouldContain("Guest Registration");
        cut.Find("p.subtitle").TextContent.ShouldContain("Please enter your personal details");

        // Assert: Verify all 7 form input fields are present
        cut.Find("input[placeholder*='John']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Doe']").ShouldNotBeNull();
        cut.Find("input[type='email']").ShouldNotBeNull();
        cut.Find("input[placeholder*='123 456']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Street']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Apartment']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Belgium']").ShouldNotBeNull();

        // Assert: Verify submit button text
        cut.Find("button[type='submit']").TextContent.Trim().ShouldBe("Continue");
    }

    [Theory]
    [InlineData("guest-first-name", "given-name")]
    [InlineData("guest-last-name", "family-name")]
    [InlineData("guest-email", "email")]
    [InlineData("guest-phone-number", "tel")]
    [InlineData("guest-address-line1", "address-line1")]
    [InlineData("guest-address-line2", "address-line2")]
    [InlineData("guest-country", "country-name")]
    public void MarkEveryFieldForAutofill(string id, string autocomplete)
    {
        // Act
        var cut = RenderWith(TestDrafts.WithRooms());

        // Assert: browsers and password managers find the field by its autocomplete token and its label
        var input = cut.Find($"#{id}");
        input.GetAttribute("autocomplete").ShouldBe(autocomplete);
        input.GetAttribute("name").ShouldBe(autocomplete);
        cut.Find($"label[for='{id}']").ShouldNotBeNull();
    }

    [Fact]
    public void GoBackToTheSearchWhenNoRoomIsPicked()
    {
        // Act: e.g. the page was opened directly or refreshed
        RenderWith(new BookingDraft());

        // Assert
        Navigation.Uri.ShouldEndWith("/search");
    }

    [Fact]
    public void StoreTheDetailsInTheDraftAndContinueToTheOverview()
    {
        // Arrange
        var draft = TestDrafts.WithRooms();
        var cut = RenderWith(draft);
        FillValidForm(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        draft.Guest.ShouldNotBeNull();
        draft.Guest.Email.ShouldBe("john.doe@example.com");
        Navigation.Uri.ShouldEndWith("/booking/overview");
    }

    [Fact]
    public void NotContinueWhenTheFormIsInvalid()
    {
        // Arrange
        var draft = TestDrafts.WithRooms();
        var cut = RenderWith(draft);

        // Act: submit the empty form
        cut.Find("form").Submit();

        // Assert
        draft.Guest.ShouldBeNull();
        Navigation.Uri.ShouldEndWith("/guests/create");
    }

    [Fact]
    public void ShowAnErrorSummaryNextToTheButtonWhenTheFormIsInvalid()
    {
        // Arrange: everything filled except the phone number
        var cut = RenderWith(TestDrafts.WithRooms());
        FillValidForm(cut);
        cut.Find("input[placeholder*='123 456']").Change("");

        // Act
        cut.Find("form").Submit();

        // Assert: the summary lists the missing field and the input is marked invalid
        cut.Find(".form-error-summary").TextContent.ShouldContain("Phone number is required.");
        cut.Find("input[placeholder*='123 456']").ClassList.ShouldContain("invalid");
    }

    [Fact]
    public void HideTheErrorSummaryOnceTheFieldsAreFixed()
    {
        // Arrange: a failed submit with a missing phone number
        var cut = RenderWith(TestDrafts.WithRooms());
        FillValidForm(cut);
        cut.Find("input[placeholder*='123 456']").Change("");
        cut.Find("form").Submit();

        // Act
        cut.Find("input[placeholder*='123 456']").Change("+32123456789");

        // Assert
        cut.FindAll(".form-error-summary").ShouldBeEmpty();
    }

    [Fact]
    public void NotShowTheErrorSummaryBeforeSubmitting()
    {
        // Act
        var cut = RenderWith(TestDrafts.WithRooms());

        // Assert
        cut.FindAll(".form-error-summary").ShouldBeEmpty();
    }

    [Fact]
    public void PrefillTheDetailsEnteredBefore()
    {
        // Arrange and Act: back from the overview with "Edit"
        var cut = RenderWith(TestDrafts.WithRooms(withGuest: true));

        // Assert
        cut.Find("input[type='email']").GetAttribute("value").ShouldBe("marie@example.com");
    }
}
