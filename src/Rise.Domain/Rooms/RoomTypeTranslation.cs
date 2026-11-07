namespace Rise.Domain.Rooms;

/// <summary>
/// texts of a <see cref="RoomType"/> in one language, owned by the room type
/// only the bed configuration for now, the name and description can follow later
/// </summary>
public class RoomTypeTranslation
{
    /// <summary>
    /// two letter code, one of <see cref="Language.Supported"/>
    /// </summary>
    public string Language { get; private set; } = string.Empty;

    private string _bedConfiguration = string.Empty;

    /// <summary>
    /// beds of the room type in this language, eg 2 single beds
    /// </summary>
    public string BedConfiguration
    {
        get => _bedConfiguration;
        internal set => _bedConfiguration = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// entity framework core constructor
    /// </summary>
    private RoomTypeTranslation()
    {
    }

    internal RoomTypeTranslation(string language, string bedConfiguration)
    {
        Language = Common.Language.EnsureSupported(language);
        BedConfiguration = bedConfiguration;
    }
}
