using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Rise.Client.Components;

/// <summary>
/// form field to pick a Font Awesome icon, use it in an EditForm with @bind-Value like InputSelect,
/// extra attributes such as id go to the select
/// </summary>
public partial class IconSelect : InputBase<string>
{
    /// <summary>
    /// text above the dropdown
    /// </summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// the icons to choose from, IconOption.Defaults when not set
    /// </summary>
    [Parameter] public IReadOnlyList<IconOption> Options { get; set; } = IconOption.Defaults;

    /// <summary>
    /// first, empty choice of the dropdown
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = "Choose an icon";

    /// <summary>
    /// optional validation message, shown under the dropdown
    /// </summary>
    [Parameter] public RenderFragment? Validation { get; set; }

    // the current value is not one of the options, for example an icon saved before
    private bool IsUnknown => !string.IsNullOrWhiteSpace(CurrentValue) && Options.All(option => option.Value != CurrentValue);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null;
        return true;
    }
}
