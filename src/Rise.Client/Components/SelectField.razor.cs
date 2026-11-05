using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// shared styled dropdown with a label and arrow, works with a plain select and with InputSelect
/// the page keeps its own select, so all binding stays on the page
/// </summary>
public partial class SelectField
{
    /// <summary>
    /// text above the dropdown
    /// </summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// the select or InputSelect itself
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// optional validation message, shown under the dropdown so it does not move the arrow
    /// </summary>
    [Parameter] public RenderFragment? Validation { get; set; }
}
