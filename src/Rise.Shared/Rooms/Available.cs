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
}
