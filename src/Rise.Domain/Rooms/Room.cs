namespace Rise.Domain.Rooms;

/// <summary>
/// A room in the hotel that can be booked by a guest.
/// </summary>
public class Room : Entity
{
    private string _roomNumber = string.Empty;
    public string RoomNumber
    {
        get => _roomNumber;
        set => _roomNumber = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    public string? Description { get; set; }

    private int _maxGuests;
    public int MaxGuests
    {
        get => _maxGuests;
        set => _maxGuests = Guard.Against.NegativeOrZero(value);
    }

    private readonly List<RoomPhoto> _photos = [];
    public IReadOnlyList<RoomPhoto> Photos => _photos.AsReadOnly();

    /// <summary>
    /// MVP: one rate per room, modelled as a collection so more rates can be added later.
    /// </summary>
    private readonly List<Rate> _rates = [];
    public IReadOnlyList<Rate> Rates => _rates.AsReadOnly();

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Room()
    {
    }

    public Room(string roomNumber, string name, int maxGuests, string? description = null)
    {
        RoomNumber = roomNumber;
        Name = name;
        MaxGuests = maxGuests;
        Description = description;
    }

    public RoomPhoto AddPhoto(string url)
    {
        var photo = new RoomPhoto(this, url);
        _photos.Add(photo);
        return photo;
    }

    public Rate AddRate(string name, decimal pricePerNight)
    {
        var rate = new Rate(this, name, pricePerNight);
        _rates.Add(rate);
        return rate;
    }
}
