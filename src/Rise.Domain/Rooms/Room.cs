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

    private readonly List<Amenity> _amenities = [];
    public IReadOnlyList<Amenity> Amenities => _amenities.AsReadOnly();

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

    /// <summary>
    /// links an amenity to the room, adding the same amenity twice does nothing
    /// </summary>
    public void AddAmenity(Amenity amenity)
    {
        Guard.Against.Null(amenity);

        if (!_amenities.Contains(amenity))
            _amenities.Add(amenity);
    }

    public RoomPhoto AddPhoto(string url)
    {
        var photo = new RoomPhoto(this, url);
        _photos.Add(photo);
        return photo;
    }

    public Rate AddRate(string name, decimal pricePerNight, string? description = null)
    {
        var rate = new Rate(this, name, pricePerNight, description);
        _rates.Add(rate);
        return rate;
    }
}
