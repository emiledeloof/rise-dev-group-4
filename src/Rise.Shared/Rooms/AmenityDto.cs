namespace Rise.Shared.Rooms;

/// <summary>
/// amenity data sent from the server to the client, same idea as RoomDto
/// </summary>
public static class AmenityDto
{
    /// <summary>
    /// one amenity of a room, eg Wi-Fi, shown in the list of the room detail page
    /// </summary>
    public class Index
    {
        // name shown to the guest, eg Wi-Fi
        public required string Name { get; set; }

        // Font Awesome icon, eg fa-wifi, an amenity can have none
        public string? Icon { get; set; }
    }
}
