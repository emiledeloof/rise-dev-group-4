namespace Rise.Domain.Rooms;

/// <summary>
/// a kind of room the estate sells, for example deluxe room, several rooms can share the same type
/// </summary>
public class RoomType : Entity
{
    private string _name = string.Empty;

    /// <summary>
    /// name shown to the admin and later to the guest, for example deluxe room
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private string? _description;

    /// <summary>
    /// what the type offers, null when the admin wrote nothing
    /// </summary>
    public string? Description
    {
        get => _description;
        set => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// entity framework core constructor
    /// </summary>
    private RoomType()
    {
    }

    /// <summary>
    /// creates a room type with a name and an optional description
    /// </summary>
    public RoomType(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
