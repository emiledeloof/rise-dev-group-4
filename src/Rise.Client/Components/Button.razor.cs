using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rise.Client.Components;

/// <summary>
/// shared site btn based on bulma, use it instead of writing btn classes by hand
/// </summary>
/// <example>
/// <code>
/// &lt;Button OnClick="SearchAsync"&gt;Search&lt;/Button&gt;
/// &lt;Button Variant="ButtonVariant.Secondary" Href="/"&gt;Back to home&lt;/Button&gt;
/// &lt;Button Type="submit" IsFullWidth="true"&gt;Continue&lt;/Button&gt;
/// </code>
/// </example>
public partial class Button
{
    /// <summary>
    /// btn label or any content inside it
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// visual style, primary by default
    /// </summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

    /// <summary>
    /// when set the btn is rendered as a link to this url
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// html type attr, button by default so it never submits a form by accident, use submit inside forms
    /// </summary>
    [Parameter] public string Type { get; set; } = "button";

    /// <summary>
    /// called on click, ignored when the btn is a link
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// takes the full width of its container, eg continue btn in the booking summary
    /// </summary>
    [Parameter] public bool IsFullWidth { get; set; }

    /// <summary>
    /// greys out the btn and blocks clicks
    /// </summary>
    [Parameter] public bool IsDisabled { get; set; }

    /// <summary>
    /// shows the bulma spinner and blocks clicks, useful while an api call is running
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// extra css classes added after the generated ones, eg mt-4
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// any other html attr passed as is, eg id or aria-label
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // builds the final class list, empty parts are skipped
    private string CssClass => string.Join(" ", new[]
    {
        "button",
        VariantClass,
        IsFullWidth ? "is-fullwidth" : null,
        IsLoading ? "is-loading" : null,
        Class,
    }.Where(c => !string.IsNullOrWhiteSpace(c)));

    // maps each variant on bulma classes, colors come from theme.css
    private string VariantClass => Variant switch
    {
        ButtonVariant.Secondary => "is-secondary",
        ButtonVariant.Danger => "is-danger is-outlined",
        _ => "is-primary",
    };
}
