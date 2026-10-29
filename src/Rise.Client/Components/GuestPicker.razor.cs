using Microsoft.AspNetCore.Components;
using Rise.Shared.Search;

namespace Rise.Client.Components;

/// <summary>
/// Elegant guest and room selector component used inside the search bar.
/// Allows specifying the number of adult guests and requested rooms with increment/decrement buttons,
/// smoothly auto-adjusting rooms or guests to ensure valid capacity (at least 1 adult per room).
/// </summary>
public partial class GuestPicker
{
    /// <summary>
    /// Current number of adult guests (two-way bindable via @bind-Value).
    /// </summary>
    [Parameter]
    public int Value { get; set; } = SearchRequest.StayPeriod.DefaultAdults;

    /// <summary>
    /// Event triggered when the guest count changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    /// <summary>
    /// Minimum number of adults required (defaults to 1).
    /// </summary>
    [Parameter]
    public int Min { get; set; } = SearchRequest.StayPeriod.MinAdults;

    /// <summary>
    /// Maximum guest capacity (0 means no upper limit enforced by picker).
    /// </summary>
    [Parameter]
    public int Max { get; set; }

    /// <summary>
    /// Optional number of rooms requested (two-way bindable via @bind-Rooms).
    /// When provided, enables the rooms counter row and displays "Guests &amp; Rooms".
    /// </summary>
    [Parameter]
    public int? Rooms { get; set; }

    /// <summary>
    /// Event triggered when the room count changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> RoomsChanged { get; set; }

    /// <summary>
    /// Minimum number of rooms required (defaults to 1).
    /// </summary>
    [Parameter]
    public int MinRooms { get; set; } = SearchRequest.StayPeriod.MinRooms;

    /// <summary>
    /// Maximum rooms available (0 means no upper limit enforced by picker).
    /// </summary>
    [Parameter]
    public int MaxRooms { get; set; }

    // Controls visibility of the dropdown counter panel
    /// <summary>
    /// Controls whether the dropdown counter panel is open.
    /// </summary>
    [Parameter]
    public bool? IsOpen { get; set; }

    /// <summary>
    /// Event triggered when the open state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    private bool _internalIsOpen;
    private bool IsOpenValue => IsOpen ?? _internalIsOpen;

    /// <summary>
    /// Clamps guest and room counts if dynamic max values loaded from the database change.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (Max > 0 && Value > Max)
        {
            Value = Max;
            _ = ValueChanged.InvokeAsync(Value);
        }

        if (HasRooms && MaxRooms > 0 && Rooms!.Value > MaxRooms)
        {
            Rooms = MaxRooms;
            _ = RoomsChanged.InvokeAsync(Rooms.Value);
        }
    }

    // Dynamic short notice displayed underneath the rooms row when an auto-adjustment occurs
    private string? _notice;

    // True when rooms row is enabled
    private bool HasRooms => Rooms.HasValue;

    // Disables increment on adults only when reaching total estate capacity
    private bool CanIncreaseAdults => Max <= 0 || Value < Max;

    // Disables decrement on adults only when reaching minimum
    private bool CanDecreaseAdults => Value > Min;

    // Disables decrement on rooms only when at minimum rooms
    private bool CanDecreaseRooms => HasRooms && Rooms!.Value > MinRooms;

    // Disables increment on rooms only when at maximum rooms
    private bool CanIncreaseRooms => HasRooms && (MaxRooms <= 0 || Rooms!.Value < MaxRooms);

    // Formatted label above the field
    private string LabelText => HasRooms ? "Guests & Rooms" : "Guests";

    // Formatted display text (e.g., "2 Adults · 1 Room" or "2 Adults")
    private string FieldText => HasRooms
        ? $"{Value} {(Value == 1 ? "Adult" : "Adults")} · {Rooms!.Value} {(Rooms!.Value == 1 ? "Room" : "Rooms")}"
        : (Value == 1 ? "1 Adult" : $"{Value} Adults");

    // Toggles the popup open or closed
    private void Toggle()
    {
        var newState = !IsOpenValue;
        _internalIsOpen = newState;
        if (IsOpenChanged.HasDelegate)
            _ = IsOpenChanged.InvokeAsync(newState);
        if (!newState)
            _notice = null;
    }

    // Closes the popup
    private void Close()
    {
        _internalIsOpen = false;
        if (IsOpenChanged.HasDelegate)
            _ = IsOpenChanged.InvokeAsync(false);
        _notice = null;
    }

    // Increments adults
    private async Task IncreaseAdults()
    {
        if (CanIncreaseAdults)
        {
            Value++;
            await ValueChanged.InvokeAsync(Value);
            _notice = null;
        }
    }

    // Decrements adults and smoothly auto-decreases rooms if adults fall below room count
    private async Task DecreaseAdults()
    {
        if (CanDecreaseAdults)
        {
            Value--;
            await ValueChanged.InvokeAsync(Value);

            // Auto-decrease rooms if adults fall below room count (at least 1 adult per room)
            if (HasRooms && Value < Rooms!.Value)
            {
                Rooms = Value;
                await RoomsChanged.InvokeAsync(Rooms.Value);
                _notice = "Room removed (min 1 adult/room)";
            }
            else
            {
                _notice = null;
            }
        }
    }

    // Increments rooms and smoothly auto-increases adults if at least 1 adult per room is required
    private async Task IncreaseRooms()
    {
        if (CanIncreaseRooms)
        {
            var newRooms = Rooms!.Value + 1;
            Rooms = newRooms;
            await RoomsChanged.InvokeAsync(newRooms);

            // Ensure at least 1 adult per room
            if (Value < newRooms)
            {
                Value = newRooms;
                await ValueChanged.InvokeAsync(Value);
                _notice = "Guest added (min 1 adult/room)";
            }
            else
            {
                _notice = null;
            }
        }
    }

    // Decrements rooms
    private async Task DecreaseRooms()
    {
        if (CanDecreaseRooms)
        {
            var newRooms = Rooms!.Value - 1;
            Rooms = newRooms;
            await RoomsChanged.InvokeAsync(newRooms);
            _notice = null;
        }
    }
}
