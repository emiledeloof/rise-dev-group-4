using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="IconSelect"/> form field
/// </summary>
public class IconSelectShould : BunitContext
{
    private sealed class Model
    {
        public string Icon { get; set; } = string.Empty;
    }

    private readonly Model _model = new();

    public IconSelectShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    private IRenderedComponent<IconSelect> RenderSelect(IReadOnlyList<IconOption>? options = null)
        => Render<IconSelect>(parameters =>
        {
            parameters
                .Add(p => p.Label, "Icon")
                .Add(p => p.Value, _model.Icon)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => _model.Icon = value))
                .Add(p => p.ValueExpression, () => _model.Icon)
                .AddCascadingValue(new EditContext(_model));
            if (options is not null)
                parameters.Add(p => p.Options, options);
        });

    [Fact]
    public void OfferTheDefaultIconsAfterTheEmptyChoice()
    {
        var cut = RenderSelect();

        var options = cut.FindAll("option");
        options[0].GetAttribute("value").ShouldBeEmpty();
        options.Skip(1).Select(o => o.GetAttribute("value")).ShouldBe(IconOption.Defaults.Select(o => o.Value));
    }

    [Fact]
    public void OfferOnlyTheGivenIcons()
    {
        var cut = RenderSelect([new IconOption("fa-bed", "Bed")]);

        cut.FindAll("option").Count.ShouldBe(2);
        cut.Find("option:last-child").TextContent.ShouldBe("Bed");
    }

    [Fact]
    public void BindTheChosenIconAndPreviewIt()
    {
        var cut = RenderSelect();

        cut.Find("select").Change("fa-wifi");

        _model.Icon.ShouldBe("fa-wifi");
        cut.Render(parameters => parameters.Add(p => p.Value, _model.Icon));
        cut.Find(".icon-select-preview i").ClassList.ShouldContain("fa-wifi");
    }

    [Fact]
    public void KeepAnIconThatIsNotInTheList()
    {
        _model.Icon = "fa-rocket";

        var cut = RenderSelect();

        cut.FindAll("option").Select(o => o.GetAttribute("value")).ShouldContain("fa-rocket");
    }

    [Fact]
    public void ShowTheLabel()
    {
        var cut = RenderSelect();

        cut.Find(".select-field > span").TextContent.ShouldBe("Icon");
    }
}
