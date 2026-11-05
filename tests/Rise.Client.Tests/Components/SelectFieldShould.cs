using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="SelectField"/> component
/// </summary>
public class SelectFieldShould : BunitContext
{
    public SelectFieldShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    [Fact]
    public void RenderTheLabelAndTheSelectInsideTheLabelElement()
    {
        var cut = Render<SelectField>(p => p
            .Add(x => x.Label, "Show")
            .AddChildContent("<select></select>"));

        cut.Find("label.select-field span").TextContent.ShouldBe("Show");
        cut.Find("label.select-field select").ShouldNotBeNull();
    }

    [Fact]
    public void HideTheArrowFromScreenReaders()
    {
        var cut = Render<SelectField>(p => p
            .Add(x => x.Label, "Show")
            .AddChildContent("<select></select>"));

        cut.Find(".select-field-control .select-field-arrow").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void RenderTheValidationOutsideTheControl()
    {
        RenderFragment validation = builder => builder.AddMarkupContent(0, "<p class=\"help\">Required</p>");

        var cut = Render<SelectField>(p => p
            .Add(x => x.Label, "Room type")
            .Add(x => x.Validation, validation)
            .AddChildContent("<select></select>"));

        cut.Find(".select-field .help").TextContent.ShouldBe("Required");
        cut.FindAll(".select-field-control .help").ShouldBeEmpty();
    }
}
