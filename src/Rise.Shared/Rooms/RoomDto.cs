namespace Rise.Shared.Rooms;

/// <summary>
/// room data sent from the server to the client, same idea as InvoiceDto
/// </summary>
public static class RoomDto
{
    /// <summary>
    /// one room free for the searched dates, what a RoomCard needs to display
    /// </summary>
    public class Available
    {
        // id of the room in the db, needed later to book it
        public required int Id { get; set; }

        // name shown as the card title
        public required string Name { get; set; }

        // short text under the title, a room can have none
        public string? Description { get; set; }

        // max nb of people in the room
        public required int MaxGuests { get; set; }

        // price for 1 night, from the room rate
        public required decimal PricePerNight { get; set; }

        // url of the first photo, a room can have none
        public string? PhotoUrl { get; set; }

        // nb of nights of the searched stay
        public required int Nights { get; set; }

        // price for the whole stay, nights times price per night
        public required decimal TotalPrice { get; set; }

        // all rates of the room for the stay, cheapest first
        public IEnumerable<RateDto.Available> Rates { get; set; } = [];
    }
}
