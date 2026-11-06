using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Shared.Rooms;
using Rise.Shared.RoomTypes;

namespace Rise.Client.Rooms;

/// <summary>admin page to list the physical rooms and create new ones</summary>
public partial class ManageRooms : ComponentBase
{
    // null while loading, an empty list once loaded with no rooms
    private IReadOnlyList<RoomAdminDto.Index>? _rooms;
    private IReadOnlyList<RoomTypeDto.Index> _roomTypes = [];

    // message shown instead of the table when the rooms or the room types could not be loaded
    private string? _loadError;

    // the create form, closed until the admin clicks new room
    private bool _isFormOpen;
    private RoomRequest.Create _createModel = NewModel();
    // kept to know when the admin changes a field, see OpenForm
    private EditContext _editContext = new(NewModel());
    private string? _formError;
    private bool _isSaving;

    // room shown in color in the table, the new one or the one that already exists
    private string? _highlightedRoomNumber;
    private bool _highlightIsConflict;

    // shown above the table after a room was created
    private string? _successMessage;

    /// <summary>reads and creates the rooms through the api</summary>
    [Inject] public required IRoomAdminService RoomAdminService { get; set; }

    /// <summary>gives the room types for the dropdown</summary>
    [Inject] public required IRoomTypeService RoomTypeService { get; set; }

    /// <summary>loads the rooms and the room types when the page opens</summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadRoomsAsync();

        var typesResult = await RoomTypeService.GetIndexAsync();
        if (!typesResult.IsSuccess)
        {
            _loadError = "The room types could not be loaded.";
            return;
        }

        _roomTypes = typesResult.Value.RoomTypes;
    }

    // ! the list is set even when the load fails, otherwise the page shows loading forever
    private async Task LoadRoomsAsync()
    {
        var roomsResult = await RoomAdminService.GetIndexAsync();
        if (!roomsResult.IsSuccess)
        {
            _loadError = "The rooms could not be loaded.";
            _rooms = [];
            return;
        }

        _rooms = roomsResult.Value;
    }

    // * green for the room just created, orange for the room that already has the number
    private string? RowClass(RoomAdminDto.Index room)
    {
        if (room.RoomNumber != _highlightedRoomNumber)
            return null;

        return _highlightIsConflict ? "room-row-conflict" : "room-row-created";
    }

    private static RoomRequest.Create NewModel() => new() { RoomNumber = "" };

    private void OpenForm()
    {
        _createModel = NewModel();
        _editContext = new EditContext(_createModel);
        _editContext.OnFieldChanged += ClearApiFeedback;
        _formError = null;
        _highlightedRoomNumber = null;
        _successMessage = null;
        _isFormOpen = true;
    }

    // * the error and the orange row belong to the last save, they go away as soon as a field changes
    private void ClearApiFeedback(object? sender, FieldChangedEventArgs e)
    {
        _formError = null;
        if (_highlightIsConflict)
            _highlightedRoomNumber = null;
    }

    private void CloseForm()
    {
        _isFormOpen = false;
        _formError = null;
    }

    private async Task CreateAsync()
    {
        _isSaving = true;
        _formError = null;
        var number = _createModel.RoomNumber.Trim();

        try
        {
            var result = await RoomAdminService.CreateAsync(_createModel);

            if (!result.IsSuccess)
            {
                _formError = result.Errors.FirstOrDefault() ?? "The room could not be created.";
                // * when the number is taken, the existing room lights up in the table
                _highlightedRoomNumber = number;
                _highlightIsConflict = true;
                return;
            }

            CloseForm();
            await LoadRoomsAsync();
            _highlightedRoomNumber = number;
            _highlightIsConflict = false;
            _successMessage = $"Room {number} created.";
        }
        finally
        {
            _isSaving = false;
        }
    }
}
