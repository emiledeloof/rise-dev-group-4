namespace Rise.Domain.Rooms;

/// <summary>
/// A photo of a <see cref="Room"/>, created via <see cref="Room.AddPhoto"/>.
/// </summary>
public class RoomPhoto : Entity
{
    public Room Room { get; private set; } = default!;

    private string _url = string.Empty;
    public string Url
    {
        get => _url;
        set => _url = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private RoomPhoto()
    {
    }

    internal RoomPhoto(Room room, string url)
    {
        Room = Guard.Against.Null(room);
        Url = url;
    }
}
