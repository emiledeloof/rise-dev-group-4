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

    private readonly List<Amenity> _amenities = [];

    /// <summary>
    /// what the type offers, also the inactive amenities, the guest only sees the active ones
    /// </summary>
    public IReadOnlyList<Amenity> Amenities => _amenities.AsReadOnly();

    private readonly List<RoomTypeTranslation> _translations = [];

    /// <summary>
    /// texts per language, a language without row has no bed configuration
    /// </summary>
    public IReadOnlyList<RoomTypeTranslation> Translations => _translations.AsReadOnly();

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

    /// <summary>
    /// links an amenity to the type, adding the same amenity twice does nothing
    /// </summary>
    public void AddAmenity(Amenity amenity)
    {
        Guard.Against.Null(amenity);

        if (!_amenities.Contains(amenity))
            _amenities.Add(amenity);
    }

    /// <summary>
    /// the type offers exactly these amenities afterwards, duplicates are ignored
    /// </summary>
    public void ReplaceAmenities(IEnumerable<Amenity> amenities)
    {
        var wanted = Guard.Against.Null(amenities).ToList();

        _amenities.RemoveAll(a => !wanted.Contains(a));
        foreach (var amenity in wanted)
            AddAmenity(amenity);
    }

    /// <summary>
    /// sets the beds in one language, an empty text removes it
    /// </summary>
    public void SetBedConfiguration(string language, string? bedConfiguration)
    {
        var code = Language.EnsureSupported(language);
        var existing = _translations.SingleOrDefault(t => t.Language == code);

        if (string.IsNullOrWhiteSpace(bedConfiguration))
        {
            if (existing is not null)
                _translations.Remove(existing);
            return;
        }

        if (existing is null)
            _translations.Add(new RoomTypeTranslation(code, bedConfiguration));
        else
            existing.BedConfiguration = bedConfiguration;
    }

    /// <summary>
    /// beds in the asked language, the default language when it has no translation, null when the type has none
    /// </summary>
    public string? BedConfigurationIn(string language)
    {
        var code = language?.Trim().ToLowerInvariant();
        return _translations.FirstOrDefault(t => t.Language == code)?.BedConfiguration
            ?? _translations.FirstOrDefault(t => t.Language == Language.Default)?.BedConfiguration;
    }
}
