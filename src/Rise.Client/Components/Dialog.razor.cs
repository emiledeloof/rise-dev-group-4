using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rise.Client.Components;

/// <summary>
/// shared modal dialog with a title, a body and an optional footer for the buttons,
/// the page owns the open state: it sets IsOpen and closes the dialog again in OnClose
/// </summary>
/// <example>
/// <code>
/// &lt;Dialog IsOpen="_model is not null" Title="Edit amenity" OnClose="CancelAsync"&gt;
///     &lt;ChildContent&gt;&lt;EditForm id="amenity-form" ...&gt;...&lt;/EditForm&gt;&lt;/ChildContent&gt;
///     &lt;Footer&gt;&lt;Button Type="submit" form="amenity-form"&gt;Save&lt;/Button&gt;&lt;/Footer&gt;
/// &lt;/Dialog&gt;
/// </code>
/// a submit button in the footer is outside the form, the html form attribute links it to the form by id
/// </example>
public partial class Dialog
{
    // unique per dialog, several dialogs on one page must not share the aria ids
    private readonly string _titleId = $"dialog-title-{Guid.NewGuid():N}";
    private readonly string _descriptionId = $"dialog-description-{Guid.NewGuid():N}";

    private ElementReference _panel;
    private bool _wasOpen;

    /// <summary>
    /// shows the dialog
    /// </summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>
    /// heading of the dialog, also its accessible name
    /// </summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// optional line under the title
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// body of the dialog, scrolls when it is higher than the screen
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// optional buttons, they stay visible at the bottom while the body scrolls
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// the user asked to close: the cross, escape or a click next to the dialog
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// max width from tablets on
    /// </summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>
    /// false keeps the dialog open on a click next to it
    /// </summary>
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>
    /// shows the cross in the top right corner
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// a question that needs an answer, read out as an alert by screen readers and shown on top of other dialogs
    /// </summary>
    [Parameter] public bool IsAlert { get; set; }

    /// <summary>
    /// extra class on the outer element, to find or style one dialog
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// moves the focus into the dialog when it opens, so the keyboard and screen readers start there
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var opened = IsOpen && !_wasOpen;
        _wasOpen = IsOpen;

        if (opened)
            await _panel.FocusAsync();
    }

    private Task CloseAsync() => OnClose.InvokeAsync();

    private Task OnBackdropClickAsync() => CloseOnBackdropClick ? CloseAsync() : Task.CompletedTask;

    private Task OnKeyDownAsync(KeyboardEventArgs args) => args.Key == "Escape" ? CloseAsync() : Task.CompletedTask;
}
