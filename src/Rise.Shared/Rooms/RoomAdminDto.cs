namespace Rise.Shared.Rooms;

/// <summary>room data for the admin manage rooms page</summary>
public static class RoomAdminDto
{
    /// <summary>one row of the rooms table, the physical room with the name of its type</summary>
    public class Index
    {
        /// <summary>id of the room in the database, used by @key in the table</summary>
        public required int Id { get; set; }

        /// <summary>room number, for example 101</summary>
        public required string RoomNumber { get; set; }

        /// <summary>name of the room type, null for an old room without type</summary>
        // TODO: make it required once every room in the database has a type
        public string? RoomTypeName { get; set; }

        /// <summary>maximum number of guests</summary>
        public required int MaxGuests { get; set; }
    }
}
