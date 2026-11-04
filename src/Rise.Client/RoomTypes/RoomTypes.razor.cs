using Microsoft.AspNetCore.Components;
using Rise.Shared.RoomTypes;

namespace Rise.Client.RoomTypes;

/// <summary>
/// admin page to create, edit and delete the room types
/// </summary>
public partial class RoomTypes : ComponentBase
{
    // null while loading
    private IReadOnlyList<RoomTypeDto.Index>? _roomTypes;
    private bool _loadFailed;

    // feedback above the table, after a save or a delete
    private string? _message;
    private bool _messageIsError;

    // create or edit model, null when the form is closed
    private RoomTypeRequest.Form? _formModel;
    private string? _formError;
    private bool _isSaving;

    // row waiting for the second click on delete
    private int? _pendingDeleteId;

    /// <summary>
    /// reads and saves the room types through the api
    /// </summary>
    [Inject] public required IRoomTypeService RoomTypeService { get; set; }

    /// <summary>
    /// loads the table when the page opens
    /// </summary>
    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var result = await RoomTypeService.GetIndexAsync();
        _loadFailed = !result.IsSuccess;

        if (_loadFailed)
        {
            ShowMessage("The room types could not be loaded.", isError: true);
            _roomTypes = [];
            return;
        }

        _roomTypes = result.Value.RoomTypes;
    }

    private void OpenCreateForm()
    {
        _pendingDeleteId = null;
        _formError = null;
        _formModel = new RoomTypeRequest.Create();
    }

    private void OpenEditForm(RoomTypeDto.Index roomType)
    {
        _pendingDeleteId = null;
        _formError = null;
        // * a copy, typing in the form does not change the table before the save
        _formModel = new RoomTypeRequest.Edit
        {
            RoomTypeId = roomType.Id,
            Name = roomType.Name,
            Description = roomType.Description,
        };
    }

    private void CloseForm()
    {
        _formModel = null;
        _formError = null;
    }

    /// <summary>
    /// only called when the validator accepts the form
    /// </summary>
    private async Task SaveAsync()
    {
        _isSaving = true;
        _formError = null;

        try
        {
            Ardalis.Result.IResult result = _formModel switch
            {
                RoomTypeRequest.Edit edit => await RoomTypeService.EditAsync(edit),
                RoomTypeRequest.Create create => await RoomTypeService.CreateAsync(create),
                _ => throw new InvalidOperationException("The form has no model."),
            };

            // form stays open so the admin can fix the name, for example a duplicate
            // * the common interface has no IsSuccess, the status says the same
            if (result.Status != ResultStatus.Ok)
            {
                _formError = FirstError(result, "The room type could not be saved.");
                return;
            }

            var name = _formModel!.Name.Trim();
            CloseForm();
            ShowMessage($"Room type '{name}' saved.", isError: false);
            await LoadAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void AskDelete(RoomTypeDto.Index roomType)
    {
        _formModel = null;
        _pendingDeleteId = roomType.Id;
    }

    private void CancelDelete() => _pendingDeleteId = null;

    private async Task DeleteAsync(RoomTypeDto.Index roomType)
    {
        _pendingDeleteId = null;
        var result = await RoomTypeService.DeleteAsync(roomType.Id);

        if (!result.IsSuccess)
        {
            ShowMessage(FirstError(result, "The room type could not be deleted."), isError: true);
            return;
        }

        ShowMessage($"Room type '{roomType.Name}' deleted.", isError: false);
        await LoadAsync();
    }

    private void ShowMessage(string message, bool isError)
    {
        _message = message;
        _messageIsError = isError;
    }

    /// <summary>
    /// message of the server, for example the name already exists, or the fallback when there is none
    /// </summary>
    private static string FirstError(Ardalis.Result.IResult result, string fallback)
        => result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error)) ?? fallback;
}
