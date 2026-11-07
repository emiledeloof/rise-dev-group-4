namespace Rise.Shared.Amenities;

/// <summary>
/// amenity data sent from the server to the client, same idea as RoomDto
/// </summary>
public static class AmenityDto
{
    /// <summary>
    /// one amenity of a room type, eg Wi-Fi, shown in the list of the room detail page
    /// </summary>
    public class Index
    {
        // name in the language of the guest, eg Wi-Fi
        public required string Name { get; set; }

        // Font Awesome icon, eg fa-wifi
        public required string Icon { get; set; }
    }

    /// <summary>
    /// one row of the admin amenities page, with what the edit form needs
    /// </summary>
    public class Admin
    {
        /// <summary>
        /// id of the amenity in the database
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// name per language code, English is always there
        /// </summary>
        public required IReadOnlyDictionary<string, string> Names { get; set; }

        /// <summary>
        /// Font Awesome icon, eg fa-wifi
        /// </summary>
        public required string Icon { get; set; }

        /// <summary>
        /// false when hidden from the guests
        /// </summary>
        public required bool IsActive { get; set; }

        /// <summary>
        /// names of the room types that offer it, sorted, empty when none
        /// </summary>
        public IReadOnlyList<string> RoomTypeNames { get; set; } = [];
    }
}
