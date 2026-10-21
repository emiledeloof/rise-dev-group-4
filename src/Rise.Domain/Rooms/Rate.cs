namespace Rise.Domain.Rooms;

/// <summary>
/// The price per night of a <see cref="Room"/> (e.g. Standard), created via <see cref="Room.AddRate"/>.
/// </summary>
public class Rate : Entity
{
    public Room Room { get; private set; } = default!;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private decimal _pricePerNight;
    public decimal PricePerNight
    {
        get => _pricePerNight;
        set => _pricePerNight = Guard.Against.NegativeOrZero(value);
    }

    // min nb of nights a guest must book with this rate, eg 1
    private int _minNights = 1;
    public int MinNights
    {
        get => _minNights;
        set => _minNights = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>
    /// conditions of the rate shown to the guest, eg no refund after booking
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Rate()
    {
    }

    internal Rate(Room room, string name, decimal pricePerNight, string? description = null)
    {
        Room = Guard.Against.Null(room);
        Name = name;
        PricePerNight = pricePerNight;
        Description = description;
    }
}
