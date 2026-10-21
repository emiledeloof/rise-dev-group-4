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

    /// <summary>
    /// one room shown as a card on the landing page, no dates so no stay price
    /// </summary>
    public class Summary
    {
        // id of the room in the db, used for the link to the detail page
        public required int Id { get; set; }

        // name shown as the card title
        public required string Name { get; set; }

        // text about the room, the card cuts it to a few lines, a room can have none
        public string? Description { get; set; }

        // max nb of people in the room
        public required int MaxGuests { get; set; }

        // "from" price for 1 night, the cheapest rate, null when the room has no rate
        public decimal? FromPrice { get; set; }

        // urls of all the photos of the room, in display order, empty when the room has none
        public IReadOnlyList<string> PhotoUrls { get; set; } = [];
    }

    /// <summary>
    /// one room shown on the room detail page, with its characteristics and amenities
    /// </summary>
    public class Detail
    {
        // id of the room in the db
        public required int Id { get; set; }

        // name shown as the page title
        public required string Name { get; set; }

        // text about what the room offers, a room can have none
        public string? Description { get; set; }

        // max nb of people in the room
        public required int MaxGuests { get; set; }

        // urls of all the photos of the room, in display order, empty when the room has none
        public IReadOnlyList<string> PhotoUrls { get; set; } = [];

        // floor area in m², null when unknown
        public int? SizeInSquareMeters { get; set; }

        // eg California king bed, null when unknown
        public string? BedType { get; set; }

        // eg Garden view, null when unknown
        public string? View { get; set; }

        // occupancy, pet and cot rules written by the hotel, null when there are none
        public string? Policies { get; set; }

        // what the room offers, sorted by name, empty when the room has none
        public IReadOnlyList<AmenityDto.Index> Amenities { get; set; } = [];
    }
}
