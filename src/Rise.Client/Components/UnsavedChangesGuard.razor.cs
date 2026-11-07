using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Rise.Client.Components;

/// <summary>
/// asks the admin to confirm before unsaved changes of a form are lost
/// put it once on the page, set IsDirty and call ConfirmDiscardAsync before closing or replacing the form
/// </summary>
public partial class UnsavedChangesGuard : ComponentBase
{
    // set while the dialog is open, completed with true when the admin discards the changes
    private TaskCompletionSource<bool>? _pending;

    /// <summary>
    /// true when the form has changes that are not saved
    /// </summary>
    [Parameter] public bool IsDirty { get; set; }

    /// <summary>
    /// true right away when nothing is dirty, otherwise opens the dialog and waits for the choice of the admin
    /// </summary>
    public Task<bool> ConfirmDiscardAsync()
    {
        if (!IsDirty)
            return Task.FromResult(true);

        // a second call while the dialog is open waits for the same choice
        _pending ??= new TaskCompletionSource<bool>();
        StateHasChanged();
        return _pending.Task;
    }

    private async Task OnBeforeInternalNavigationAsync(LocationChangingContext context)
    {
        if (!await ConfirmDiscardAsync())
            context.PreventNavigation();
    }

    private void Choose(bool discard)
    {
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(discard);
    }
}
