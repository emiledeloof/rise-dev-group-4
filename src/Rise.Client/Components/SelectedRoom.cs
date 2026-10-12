namespace Rise.Client.Components;

/// <summary>
/// one line of the selection summary, a room the guest picked with its rate
/// built by the results page from the selection and the rooms of the api
/// </summary>
public record SelectedRoom(string RoomName, string RateName, decimal TotalPrice);