using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="Button"/> component
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// cut means component under test, it is the rendered btn we inspect like a mini html page
/// </summary>
public class ButtonShould : BunitContext
{
    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public ButtonShould(ITestOutputHelper outputHelper)
    {
        // sends the app logs to the test output, handy when a test fails
        Services.AddXunitLogger(outputHelper);
    }

    /// <summary>
    /// 
    /// no variant given means primary
    /// </summary>
    [Fact]
    public void RenderAsPrimaryByDefault()
    {
        // arrange and act, render the btn with only a label, no variant
        // AddChildContent sets what goes between the tags, like <Button>Search</Button>
        var cut = Render<Button>(p => p.AddChildContent("Search"));

        // assert, Find returns the first html element matching the css selector
        var button = cut.Find("button");
        button.ClassList.ShouldContain("button");       // bulma base class
        button.ClassList.ShouldContain("is-primary");   // default variant
        button.TextContent.Trim().ShouldBe("Search");    // Trim removes spaces around the label
    }

    /// <summary>
    /// default type must not submit forms by accident
    /// </summary>
    [Fact]
    public void UseButtonTypeByDefault()
    {
        // arrange and act
        var cut = Render<Button>(p => p.AddChildContent("Search"));

        // assert, GetAttribute reads an html attr, here type="button"
        cut.Find("button").GetAttribute("type").ShouldBe("button");
    }

    /// <summary>
    /// each variant maps on its own css classes
    /// Theory runs the same test once per InlineData line, here 2 runs
    /// </summary>
    [Theory]
    [InlineData(ButtonVariant.Secondary, "is-secondary")]
    [InlineData(ButtonVariant.Danger, "is-outlined")]
    public void ApplyVariantClass(ButtonVariant variant, string expectedClass)
    {
        // arrange and act, Add sets a component parameter, like Variant="..." in razor
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, variant)
            .AddChildContent("Cancel"));

        // assert, the class that matches the variant is there
        cut.Find("button").ClassList.ShouldContain(expectedClass);
    }

    /// <summary>
    /// href turns the btn into a link
    /// </summary>
    [Fact]
    public void RenderAsLinkWhenHrefIsSet()
    {
        // arrange and act, give an href so the component should render an a tag
        var cut = Render<Button>(p => p
            .Add(x => x.Href, "/rooms")
            .AddChildContent("View rooms"));

        // assert, FindAll returns every match, no button tag at all
        cut.FindAll("button").ShouldBeEmpty();

        // and the link points to the given url
        cut.Find("a").GetAttribute("href").ShouldBe("/rooms");
    }

    /// <summary>
    /// click callback is called once per click
    /// </summary>
    [Fact]
    public void CallOnClickWhenClicked()
    {
        // arrange, a counter that goes up each time OnClick is called
        var clicks = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.OnClick, () => { clicks++; })
            .AddChildContent("Search"));

        // act, Click simulates a user click on the element
        cut.Find("button").Click();

        // assert, the callback ran exactly once
        clicks.ShouldBe(1);
    }

    /// <summary>
    /// disabled and loading both block clicks
    /// </summary>
    [Fact]
    public void BeDisabledWhileLoading()
    {
        // arrange and act, render the btn in loading state
        var cut = Render<Button>(p => p
            .Add(x => x.IsLoading, true)
            .AddChildContent("Search"));

        // assert, disabled attr blocks clicks and is-loading shows the bulma spinner
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
        // arrange and act, an extra class plus the full width option
        var cut = Render<Button>(p => p
            .Add(x => x.Class, "mt-4")
            .Add(x => x.IsFullWidth, true)
            .AddChildContent("Continue"));

        // assert, both the extra class and the generated one are present
        var button = cut.Find("button");
        button.ClassList.ShouldContain("mt-4");
        button.ClassList.ShouldContain("is-fullwidth");
    }
}
