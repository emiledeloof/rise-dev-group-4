using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Shared.Guests;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Guests;

/// <summary>
/// Component tests for the Guest registration page (<see cref="Create"/>) using bUnit.
/// Verifies proper rendering of input fields, validation integration, and submit action.
/// Follows naming convention &lt;Subject&gt;Should.
/// </summary>
public class CreateGuestShould : BunitContext
{
    private readonly FakeGuestService _fakeGuestService = new();

    public CreateGuestShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddScoped<IGuestService>(_ => _fakeGuestService);
    }

    [Fact]
    public void RenderRegistrationFormWithAllFields()
    {
        // Act: Render the component under test (cut)
        var cut = Render<Create>();

        // Assert: Verify title and subtitle
        cut.Find("h2.title").TextContent.ShouldContain("Guest Registration");
        cut.Find("p.subtitle").TextContent.ShouldContain("Please enter your personal details");

        // Assert: Verify all 7 required form input fields are present
        cut.Find("input[placeholder*='John']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Doe']").ShouldNotBeNull();
        cut.Find("input[type='email']").ShouldNotBeNull();
        cut.Find("input[placeholder*='123 456']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Street']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Apartment']").ShouldNotBeNull();
        cut.Find("input[placeholder*='Belgium']").ShouldNotBeNull();

        // Assert: Verify submit button text
        var submitButton = cut.Find("button[type='submit']");
        submitButton.ShouldNotBeNull();
        submitButton.TextContent.Trim().ShouldBe("Register Guest");
    }

    [Fact]
    public void DisplayErrorNotificationWhenApiReturnsFailure()
    {
        // Arrange: Configure fake service to return a conflict error
        _fakeGuestService.ResultToReturn = Ardalis.Result.Result.Conflict("A guest with this email already exists.");

        // Act: Render component and simulate form submission
        var cut = Render<Create>();
        
        // Fill out valid fields so client-side FluentValidation passes
        cut.Find("input[placeholder*='John']").Change("John");
        cut.Find("input[placeholder*='Doe']").Change("Doe");
        cut.Find("input[type='email']").Change("duplicate@example.com");
        cut.Find("input[placeholder*='123 456']").Change("+32123456789");
        cut.Find("input[placeholder*='Street']").Change("Kerkstraat 1");
        cut.Find("input[placeholder*='Belgium']").Change("Belgium");

        // Submit form
        cut.Find("form").Submit();

        // Assert: Error banner is displayed
        var notification = cut.Find(".notification.is-danger");
        notification.ShouldNotBeNull();
        notification.TextContent.ShouldContain("A guest with this email already exists.");
    }
}
