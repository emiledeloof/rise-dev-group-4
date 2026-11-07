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

    private int? _sizeInSquareMeters;
    /// <summary>
    /// floor area of the room, null when unknown
    /// </summary>
    public int? SizeInSquareMeters
    {
        get => _sizeInSquareMeters;
        set => _sizeInSquareMeters = value is null ? null : Guard.Against.NegativeOrZero(value.Value);
    }

    private string? _bedType;
    /// <summary>
    /// e.g. California king bed or 2 single beds, null when unknown
    /// </summary>
    public string? BedType
    {
        get => _bedType;
        set => _bedType = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string? _view;
    /// <summary>
    /// e.g. Garden view, null when unknown
    /// </summary>
    public string? View
    {
        get => _view;
        set => _view = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string? _policies;
    /// <summary>
    /// free text with the occupancy, pet and cot rules of the room, null when the hotel wrote none
    /// </summary>
    public string? Policies
    {
        get => _policies;
        set => _policies = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// type of the room, several rooms can share the same type, for example two deluxe rooms 104 and 105
    /// </summary>
    public RoomType RoomType { get; private set; } = default!;

    private readonly List<Amenity> _amenities = [];
    public IReadOnlyList<Amenity> Amenities => _amenities.AsReadOnly();

    private readonly List<RoomPhoto> _photos = [];
    public IReadOnlyList<RoomPhoto> Photos => _photos.AsReadOnly();

    /// <summary>
    /// A room can have multiple rates, including rates with overlapping availability periods.
    /// </summary>
    private readonly List<Rate> _rates = [];
    public IReadOnlyList<Rate> Rates => _rates.AsReadOnly();

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Room()
    {
    }

    public Room(string roomNumber, string name, int maxGuests, RoomType roomType, string? description = null)
    {
        RoomNumber = roomNumber;
        Name = name;
        MaxGuests = maxGuests;
        // ! a new room always has a type
        RoomType = Guard.Against.Null(roomType);
        Description = description;
    }

    /// <summary>
    /// links an amenity to the room, adding the same amenity twice does nothing
    /// </summary>
    public void AddAmenity(Amenity amenity)
    {
        Guard.Against.Null(amenity);

        if (!_amenities.Contains(amenity))
            _amenities.Add(amenity);
    }

    /// <summary>
    /// links the room to a type, a room has one type at a time
    /// </summary>
    public void ChangeType(RoomType roomType)
    {
        RoomType = Guard.Against.Null(roomType);
    }

    public RoomPhoto AddPhoto(string url)
    {
        var photo = new RoomPhoto(this, url);
        _photos.Add(photo);
        return photo;
    }

    public Rate AddRate(string name, decimal pricePerNight, string? description = null, DateOnly? validFrom = null, DateOnly? validTo = null)
    {
        var rate = new Rate(this, name, pricePerNight, description, validFrom, validTo);
        _rates.Add(rate);
        return rate;
    }
}
