using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="FilterBar"/> component
/// </summary>
public class FilterBarShould : BunitContext
{
    public FilterBarShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
    }

    [Fact]
    public void RenderItsFiltersDirectlyInTheGrid()
    {
        var cut = Render<FilterBar>(p => p.AddChildContent("<label></label><label></label>"));

        cut.FindAll(".filter-bar > label").Count.ShouldBe(2);
    }
}
