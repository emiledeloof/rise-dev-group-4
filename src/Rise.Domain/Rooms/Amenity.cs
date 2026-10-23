namespace Rise.Domain.Rooms;

/// <summary>
/// Something a <see cref="Room"/> offers (e.g. Wi-Fi, Minibar).
/// Shared by all rooms that offer it, so a rename changes it everywhere.
/// </summary>
public class Amenity : Entity
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// Font Awesome icon shown next to the name, e.g. fa-wifi, an amenity can have none
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Amenity()
    {
    }

    public Amenity(string name, string? icon = null)
    {
        Name = name;
        Icon = icon;
    }
}
