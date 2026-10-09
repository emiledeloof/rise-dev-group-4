using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Pages;

/// <summary>
/// These tests are written entirely in C#.
/// Learn more at https://bunit.dev/docs/getting-started/writing-tests.html#creating-basic-tests-in-cs-files
/// </summary>
public class IndexShould : BunitContext
{
    public IndexShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    /// <summary>
    /// the hero shows the title of the mockup, it replaced the hello world of the template
    /// </summary>
    [Fact]
    public void ShowTheHeroTitle()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert
        cut.Find("h1").TextContent.ShouldBe("Slow days among vines and granite");
    }

    /// <summary>
    /// the hero holds the shared search bar, so the guest can search a stay from the home
    /// </summary>
    [Fact]
    public void ShowTheSearchBar()
    {
        // Arrange
        var cut = Render<Index>();

        // Assert, one search bar inside the hero
        cut.FindAll(".hero .search-bar").Count.ShouldBe(1);
    }
}