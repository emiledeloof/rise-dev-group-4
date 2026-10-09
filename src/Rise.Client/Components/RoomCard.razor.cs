using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// card showing one room in the search results, photo, name, description, capacity, price and a select btn
/// </summary>
public partial class RoomCard
{
    /// <summary>
    /// name of the room, eg vigna suite
    /// </summary>
    [Parameter] public required string Name { get; set; }

    /// <summary>
    /// short text about the room, can be empty
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// max nb of guests in the room, eg 2
    /// </summary>
    [Parameter] public int MaxGuests { get; set; }

    /// <summary>
    /// price for one night in euros, eg 160
    /// </summary>
    [Parameter] public decimal PricePerNight { get; set; }

    /// <summary>
    /// nb of nights for the stay
    /// </summary>
    [Parameter] public int Nights { get; set; }

    /// <summary>
    /// price for the whole stay
    /// </summary>
    [Parameter] public decimal TotalPrice { get; set; }

    /// <summary>
    /// url of the main photo of the room, can be empty if the room has no photo yet
    /// </summary>
    [Parameter] public string? PhotoUrl { get; set; }

    /// <summary>
    /// called when the guest clicks select, the page decides what happens next
    /// same idea as CheckInChanged in DateRangePicker, the card only tells the page
    /// </summary>
    [Parameter] public EventCallback OnSelect { get; set; }

}
