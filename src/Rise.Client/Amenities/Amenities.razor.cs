using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Client.Components;
using Rise.Shared.Amenities;
using Rise.Shared.Common;

namespace Rise.Client.Amenities;

/// <summary>
/// admin page to create, edit, activate and deactivate the amenities
/// </summary>
public partial class Amenities : ComponentBase
{
    // null while loading
    private IReadOnlyList<AmenityDto.Admin>? _amenities;
    private bool _loadFailed;

    // feedback above the table, after a save or a status change
    private string? _message;
    private bool _messageIsError;

    // create or edit model, null when the form is closed
    private AmenityRequest.Form? _formModel;
    private string? _formSnapshot;
    private string? _formError;
    private bool _isSaving;

    // amenities whose switch waits for the server, a second click is ignored meanwhile
    private readonly HashSet<int> _pendingStatusChanges = [];

    // raised when the server refuses a change, the switches render again with the saved state
    private int _switchVersion;

    private UnsavedChangesGuard _guard = default!;

    /// <summary>
    /// reads and saves the amenities through the api
    /// </summary>
    [Inject] public required IAmenityService AmenityService { get; set; }

    // the form differs from what it was when it opened
    private bool IsDirty => _formModel is not null && Snapshot(_formModel) != _formSnapshot;

    /// <summary>
    /// loads the table when the page opens
    /// </summary>
    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var result = await AmenityService.GetIndexAsync();
        _loadFailed = !result.IsSuccess;

        if (_loadFailed)
        {
            ShowMessage("The amenities could not be loaded.", isError: true);
            _amenities = [];
            return;
        }

        _amenities = result.Value.Amenities;
    }

    private Task OpenCreateFormAsync()
        => OpenFormAsync(new AmenityRequest.Create { Names = EmptyNames(), Icon = string.Empty });

    private Task OpenEditFormAsync(AmenityDto.Admin amenity)
    {
        // * a copy, typing in the form does not change the table before the save
        var names = EmptyNames();
        foreach (var (language, name) in amenity.Names)
            names[language] = name;

        return OpenFormAsync(new AmenityRequest.Edit
        {
            AmenityId = amenity.Id,
            Names = names,
            Icon = amenity.Icon,
        });
    }

    private async Task OpenFormAsync(AmenityRequest.Form form)
    {
        if (!await _guard.ConfirmDiscardAsync())
            return;

        _formError = null;
        _formModel = form;
        _formSnapshot = Snapshot(form);
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
                AmenityRequest.Edit edit => await AmenityService.EditAsync(edit),
                AmenityRequest.Create create => await AmenityService.CreateAsync(create),
                _ => throw new InvalidOperationException("The form has no model."),
            };

            // form stays open so the admin can fix the name, for example a duplicate
            if (result.Status != ResultStatus.Ok)
            {
                _formError = FirstError(result, "The amenity could not be saved.");
                return;
            }

            var name = _formModel!.Names[Languages.Default]!.Trim();
            CloseForm();
            ShowMessage($"Amenity '{name}' saved.", isError: false);
            await LoadAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task SetActiveAsync(AmenityDto.Admin amenity, ChangeEventArgs args)
    {
        if (args.Value is not bool isActive || isActive == amenity.IsActive || !_pendingStatusChanges.Add(amenity.Id))
            return;

        try
        {
            var result = isActive
                ? await AmenityService.ActivateAsync(amenity.Id)
                : await AmenityService.DeactivateAsync(amenity.Id);

            if (!result.IsSuccess)
            {
                _switchVersion++;
                ShowMessage(FirstError(result, "The amenity could not be changed."), isError: true);
                return;
            }

            ShowMessage(isActive
                ? $"'{EnglishName(amenity)}' is shown to guests again."
                : $"'{EnglishName(amenity)}' is hidden from guests. It stays ticked on its room types.", isError: false);
            await LoadAsync();
        }
        finally
        {
            _pendingStatusChanges.Remove(amenity.Id);
        }
    }

    private void ShowMessage(string message, bool isError)
    {
        _message = message;
        _messageIsError = isError;
    }

    private static string EnglishName(AmenityDto.Admin amenity)
        => amenity.Names.GetValueOrDefault(Languages.Default) ?? "—";

    private static int TranslatedCount(AmenityDto.Admin amenity)
        => Languages.Supported.Count(language => amenity.Names.ContainsKey(language));

    // every language in the dictionary, so each input has an entry to write to
    private static Dictionary<string, string?> EmptyNames()
        => Languages.Supported.ToDictionary(language => language, string? (_) => string.Empty);

    // json of the form, compared to know if the admin changed something
    private static string Snapshot(AmenityRequest.Form form) => JsonSerializer.Serialize(form, form.GetType());

    /// <summary>
    /// message of the server, for example the name already exists, or the fallback when there is none
    /// </summary>
    private static string FirstError(Ardalis.Result.IResult result, string fallback)
        => result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error)) ?? fallback;
}
