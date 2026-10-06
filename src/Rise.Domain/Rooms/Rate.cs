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

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Rate()
    {
    }

    internal Rate(Room room, string name, decimal pricePerNight)
    {
        Room = Guard.Against.Null(room);
        Name = name;
        PricePerNight = pricePerNight;
    }
}
