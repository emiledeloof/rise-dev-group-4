namespace Rise.Shared.Rooms;

/// <summary>
/// responses sent by the room endpoints, same idea as InvoiceResponse
/// </summary>
public static partial class RoomResponse
{
    /// <summary>
    /// what the availability endpoint sends back, the list of free rooms
    /// </summary>
    public class Available
    {
        // list of free rooms, empty list by default so it's never null
        public IEnumerable<RoomDto.Available> Rooms { get; set; } = [];
    }

    /// <summary>
    /// what the room list endpoint sends back, the first rooms and how many rooms there are in total
    /// </summary>
    public class List
    {
        // the rooms asked for, at most the Take of the request, empty list by default so it's never null
        public IEnumerable<RoomDto.Summary> Rooms { get; set; } = [];

        // nb of rooms that are not deleted, so the page knows if there are more than it shows
        public int TotalCount { get; set; }
    }
}
