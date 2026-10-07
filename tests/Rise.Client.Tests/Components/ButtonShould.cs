using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="Button"/> component
/// </summary>
public class ButtonShould : BunitContext
{
    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public ButtonShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    /// <summary>
    /// no variant given means primary
    /// </summary>
    [Fact]
    public void RenderAsPrimaryByDefault()
    {
        var cut = Render<Button>(p => p.AddChildContent("Search"));

        var button = cut.Find("button");
        button.ClassList.ShouldContain("button");
        button.ClassList.ShouldContain("is-primary");
        button.TextContent.Trim().ShouldBe("Search");
    }

    /// <summary>
    /// default type must not submit forms by accident
    /// </summary>
    [Fact]
    public void UseButtonTypeByDefault()
    {
        var cut = Render<Button>(p => p.AddChildContent("Search"));

        cut.Find("button").GetAttribute("type").ShouldBe("button");
    }

    /// <summary>
    /// each variant maps on its own css classes
    /// </summary>
    [Theory]
    [InlineData(ButtonVariant.Secondary, "is-secondary")]
    [InlineData(ButtonVariant.Danger, "is-outlined")]
    public void ApplyVariantClass(ButtonVariant variant, string expectedClass)
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, variant)
            .AddChildContent("Cancel"));

        cut.Find("button").ClassList.ShouldContain(expectedClass);
    }

    /// <summary>
    /// href turns the btn into a link
    /// </summary>
    [Fact]
    public void RenderAsLinkWhenHrefIsSet()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Href, "/rooms")
            .AddChildContent("View rooms"));

        cut.FindAll("button").ShouldBeEmpty();
        cut.Find("a").GetAttribute("href").ShouldBe("/rooms");
    }

    /// <summary>
    /// click callback is called once per click
    /// </summary>
    [Fact]
    public void CallOnClickWhenClicked()
    {
        var clicks = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.OnClick, () => { clicks++; })
            .AddChildContent("Search"));

        cut.Find("button").Click();

        clicks.ShouldBe(1);
    }

    /// <summary>
    /// disabled and loading both block clicks
    /// </summary>
    [Fact]
    public void BeDisabledWhileLoading()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.IsLoading, true)
            .AddChildContent("Search"));

        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        button.ClassList.ShouldContain("is-loading");
    }

    /// <summary>
    /// extra classes are kept next to the generated ones
    /// </summary>
    [Fact]
    public void KeepExtraClasses()
    {
        var cut = Render<Button>(p => p
            .Add(x => x.Class, "mt-4")
            .Add(x => x.IsFullWidth, true)
            .AddChildContent("Continue"));

        var button = cut.Find("button");
        button.ClassList.ShouldContain("mt-4");
        button.ClassList.ShouldContain("is-fullwidth");
    }
}
