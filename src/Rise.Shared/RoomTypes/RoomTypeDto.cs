namespace Rise.Shared.RoomTypes;

/// <summary>
/// room type data sent from the server to the client
/// </summary>
public static class RoomTypeDto
{
    /// <summary>
    /// one row of the admin room types page, with what the edit form needs
    /// </summary>
    public class Index
    {
        /// <summary>
        /// id of the room type in the database
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// name of the type, for example deluxe room
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// what the type offers, null when empty
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// numbers of the physical rooms of this type, for example 101, empty when no room uses it
        /// </summary>
        public IReadOnlyList<string> RoomNumbers { get; set; } = [];
    }
}
