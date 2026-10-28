using Microsoft.AspNetCore.Components;
using Rise.Shared.Search;

namespace Rise.Client.Components;

/// <summary>
/// Elegant guest selector component used inside the search bar.
/// Allows specifying the number of adult guests with increment/decrement buttons,
/// enforcing minimum (1 adult) and maximum resort capacity limits.
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
    /// Maximum guest capacity (defaults to resort capacity, 6).
    /// </summary>
    [Parameter]
    public int Max { get; set; } = SearchRequest.StayPeriod.MaxCapacity;

    // Controls visibility of the dropdown counter panel
    private bool _isOpen;

    // Disables the decrement button when at minimum limit
    private bool CanDecreaseAdults => Value > Min;

    // Disables the increment button when at maximum capacity
    private bool CanIncreaseAdults => Value < Max;

    // Formatted display text (e.g., "1 Adult" or "2 Adults")
    private string FieldText => Value == 1 ? "1 Adult" : $"{Value} Adults";

    // Toggles the popup open or closed
    private void Toggle() => _isOpen = !_isOpen;

    // Closes the popup
    private void Close() => _isOpen = false;

    // Increments the guest count if below maximum
    private async Task IncreaseAdults()
    {
        if (CanIncreaseAdults)
        {
            Value++;
            await ValueChanged.InvokeAsync(Value);
        }
    }

    // Decrements the guest count if above minimum
    private async Task DecreaseAdults()
    {
        if (CanDecreaseAdults)
        {
            Value--;
            await ValueChanged.InvokeAsync(Value);
        }
    }
}
