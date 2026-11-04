namespace Rise.Shared.RoomTypes;

public static partial class RoomTypeResponse
{
    /// <summary>
    /// all room types for the admin page, no paging because the estate only has a few
    /// </summary>
    public class Index
    {
        /// <summary>
        /// room types sorted by name
        /// </summary>
        public required IReadOnlyList<RoomTypeDto.Index> RoomTypes { get; init; }
    }
}
