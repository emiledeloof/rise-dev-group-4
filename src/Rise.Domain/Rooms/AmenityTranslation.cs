namespace Rise.Domain.Rooms;

/// <summary>
/// name of an <see cref="Amenity"/> in one language, owned by the amenity, created via <see cref="Amenity.SetName"/>
/// </summary>
public class AmenityTranslation
{
    /// <summary>
    /// two letter code, one of <see cref="Language.Supported"/>
    /// </summary>
    public string Language { get; private set; } = string.Empty;

    private string _name = string.Empty;

    /// <summary>
    /// name shown to the guest in this language, eg Wi-Fi
    /// </summary>
    public string Name
    {
        get => _name;
        internal set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// entity framework core constructor
    /// </summary>
    private AmenityTranslation()
    {
    }

    internal AmenityTranslation(string language, string name)
    {
        Language = Common.Language.EnsureSupported(language);
        Name = name;
    }
}
