namespace Rise.Client.Components;

/// <summary>
/// one line of the selection summary, a room the guest picked with its rate
/// built by the results page from the selection and the rooms of the api
/// TotalPrice is the price the guest pays (incl. VAT when enabled)
/// TotalPriceExclVat comes from the server for information only, null when VAT is disabled or 0
/// </summary>
public record SelectedRoom(string RoomName, string RateName, decimal TotalPrice, decimal? TotalPriceExclVat = null);
