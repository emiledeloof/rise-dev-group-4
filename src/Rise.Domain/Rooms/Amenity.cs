namespace Rise.Domain.Rooms;

/// <summary>
/// Something a <see cref="RoomType"/> offers (e.g. Wi-Fi, Minibar).
/// Shared by all room types that offer it, so a rename changes it everywhere.
/// </summary>
public class Amenity : Entity
{
    private readonly List<AmenityTranslation> _translations = [];

    /// <summary>
    /// name per language, the default language is always there
    /// </summary>
    public IReadOnlyList<AmenityTranslation> Translations => _translations.AsReadOnly();

    private string _icon = string.Empty;

    /// <summary>
    /// Font Awesome icon shown next to the name, e.g. fa-wifi
    /// </summary>
    public string Icon
    {
        get => _icon;
        set => _icon = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// an inactive amenity is hidden from the guests but stays assigned to its room types
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Amenity()
    {
    }

    /// <summary>
    /// creates an active amenity with its name in the default language
    /// </summary>
    public Amenity(string defaultName, string icon)
    {
        SetName(Language.Default, defaultName);
        Icon = icon;
    }

    /// <summary>
    /// name in the default language, always present
    /// </summary>
    public string DefaultName => NameIn(Language.Default);

    /// <summary>
    /// sets the name in one language, an empty name removes the translation
    /// the default language can never be removed
    /// </summary>
    public void SetName(string language, string? name)
    {
        var code = Language.EnsureSupported(language);
        var existing = _translations.SingleOrDefault(t => t.Language == code);

        if (string.IsNullOrWhiteSpace(name))
        {
            if (code == Language.Default)
                throw new ArgumentException("The name in the default language is required.", nameof(name));

            if (existing is not null)
                _translations.Remove(existing);
            return;
        }

        if (existing is null)
            _translations.Add(new AmenityTranslation(code, name));
        else
            existing.Name = name;
    }

    /// <summary>
    /// name in the asked language, the default language name when it has no translation
    /// </summary>
    public string NameIn(string language)
    {
        var code = language?.Trim().ToLowerInvariant();
        return _translations.FirstOrDefault(t => t.Language == code)?.Name
            ?? _translations.First(t => t.Language == Language.Default).Name;
    }

    /// <summary>
    /// shows the amenity to the guests again
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// hides the amenity from the guests, its assignments are kept
    /// </summary>
    public void Deactivate() => IsActive = false;
}
