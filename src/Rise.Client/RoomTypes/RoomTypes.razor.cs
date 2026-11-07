using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Client.Components;
using Rise.Shared.Amenities;
using Rise.Shared.Common;
using Rise.Shared.RoomTypes;

namespace Rise.Client.RoomTypes;

/// <summary>
/// admin page to create, edit and delete the room types, with their amenities and bed configuration
/// </summary>
public partial class RoomTypes : ComponentBase
{
    // null while loading
    private IReadOnlyList<RoomTypeDto.Index>? _roomTypes;
    private bool _loadFailed;

    // the catalogue of amenities, active and inactive, empty when it could not be loaded
    private IReadOnlyList<AmenityDto.Admin> _amenities = [];

    // feedback above the table, after a save or a delete
    private string? _message;
    private bool _messageIsError;

    // create or edit model, null when the form is closed
    private RoomTypeRequest.Form? _formModel;
    private string? _formSnapshot;
    private string? _formError;
    private bool _isSaving;

    // row waiting for the second click on delete
    private int? _pendingDeleteId;

    private UnsavedChangesGuard _guard = default!;

    /// <summary>
    /// reads and saves the room types through the api
    /// </summary>
    [Inject] public required IRoomTypeService RoomTypeService { get; set; }

    /// <summary>
    /// reads the amenities the admin can tick
    /// </summary>
    [Inject] public required IAmenityService AmenityService { get; set; }

    // the form differs from what it was when it opened
    private bool IsDirty => _formModel is not null && Snapshot(_formModel) != _formSnapshot;

    // only the active amenities can be ticked, the inactive ones are hidden from the guests anyway
    private IReadOnlyList<AmenityDto.Admin> ActiveAmenities => _amenities.Where(a => a.IsActive).ToList();

    // inactive amenities still assigned to the open type, kept by the server when saving
    private IReadOnlyList<string> InactiveAssignedNames => _formModel is null
        ? []
        : _amenities.Where(a => !a.IsActive && _formModel.AmenityIds.Contains(a.Id)).Select(NameOf).ToList();

    /// <summary>
    /// loads the table and the amenities when the page opens
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();

        var amenities = await AmenityService.GetIndexAsync();
        if (amenities.IsSuccess)
            _amenities = amenities.Value.Amenities;
    }

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

    private Task OpenCreateFormAsync()
        => OpenFormAsync(() => Task.FromResult<RoomTypeRequest.Form?>(new RoomTypeRequest.Create { BedConfigurations = EmptyTexts() }));

    private Task OpenEditFormAsync(RoomTypeDto.Index roomType)
        => OpenFormAsync(async () =>
        {
            // the list has no amenities and beds, the edit form asks for them
            var result = await RoomTypeService.GetForEditAsync(roomType.Id);
            if (!result.IsSuccess)
            {
                ShowMessage(FirstError(result, "The room type could not be loaded."), isError: true);
                return null;
            }

            // * a copy, typing in the form does not change the table before the save
            var edit = result.Value;
            var beds = EmptyTexts();
            foreach (var (language, bed) in edit.BedConfigurations)
                beds[language] = bed;

            return new RoomTypeRequest.Edit
            {
                RoomTypeId = edit.Id,
                Name = edit.Name,
                Description = edit.Description,
                AmenityIds = edit.AmenityIds.ToList(),
                BedConfigurations = beds,
            };
        });

    private async Task OpenFormAsync(Func<Task<RoomTypeRequest.Form?>> createForm)
    {
        if (!await _guard.ConfirmDiscardAsync())
            return;

        _pendingDeleteId = null;
        _formError = null;
        var form = await createForm();
        _formModel = form;
        _formSnapshot = form is null ? null : Snapshot(form);
    }

    private async Task CancelFormAsync()
    {
        if (await _guard.ConfirmDiscardAsync())
            CloseForm();
    }

    private void CloseForm()
    {
        _formModel = null;
        _formSnapshot = null;
        _formError = null;
    }

    private void ToggleAmenity(int amenityId, bool isTicked)
    {
        _formModel!.AmenityIds.Remove(amenityId);
        if (isTicked)
            _formModel.AmenityIds.Add(amenityId);
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

    private async Task AskDeleteAsync(RoomTypeDto.Index roomType)
    {
        if (!await _guard.ConfirmDiscardAsync())
            return;

        CloseForm();
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

    private static string NameOf(AmenityDto.Admin amenity)
        => amenity.Names.GetValueOrDefault(Languages.Default) ?? "—";

    // every language in the dictionary, so each input has an entry to write to
    private static Dictionary<string, string?> EmptyTexts()
        => Languages.Supported.ToDictionary(language => language, string? (_) => string.Empty);

    // json of the form, compared to know if the admin changed something, the ids are sorted so ticking and unticking is no change
    private static string Snapshot(RoomTypeRequest.Form form)
        => JsonSerializer.Serialize(new { form.Name, form.Description, Ids = form.AmenityIds.Order(), form.BedConfigurations });

    /// <summary>
    /// message of the server, for example the name already exists, or the fallback when there is none
    /// </summary>
    private static string FirstError(Ardalis.Result.IResult result, string fallback)
        => result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error)) ?? fallback;
}
