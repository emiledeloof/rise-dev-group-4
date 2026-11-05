using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// shared row of filters above a list, stacked on phones and 3 columns from tablet size
/// </summary>
public partial class FilterBar
{
    /// <summary>
    /// the filters, each one fills a column of the grid
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
