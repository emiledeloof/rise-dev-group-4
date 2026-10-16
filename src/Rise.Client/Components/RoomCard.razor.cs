using Microsoft.AspNetCore.Components;
using Rise.Shared.Rooms;

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
    /// all rates of the room for the stay, cheapest first, from RoomDto.Available
    /// </summary>
    [Parameter] public IEnumerable<RateDto.Available> Rates { get; set; } = [];
    /// <summary>
    /// url of the main photo of the room, can be empty if the room has no photo yet
    /// </summary>
    [Parameter] public string? PhotoUrl { get; set; }

    /// <summary>
    /// url of the room detail page, eg rooms/3?checkIn=2026-10-14&amp;checkOut=2026-10-17
    /// no link is shown when it is empty
    /// </summary>
    [Parameter] public string? DetailsUrl { get; set; }

    /// <summary>
    /// id of the rate chosen for this room, null when the room is not in the selection
    /// the page owns the selection, the card only shows it
    /// </summary>
    [Parameter] public int? SelectedRateId { get; set; }

    /// <summary>
    /// called with the id of the rate the guest picks, the page updates its selection
    /// same idea as CheckInChanged in DateRangePicker, the card only tells the page
    /// </summary>
    [Parameter] public EventCallback<int> OnRateSelected { get; set; }

    /// <summary>
    /// called when the guest takes the room out of the selection
    /// </summary>
    [Parameter] public EventCallback OnRemove { get; set; }

    // select btn, picks the cheapest rate, the 1st one since the server sorts them
    private Task SelectCheapestAsync() => OnRateSelected.InvokeAsync(Rates.First().Id);

}
