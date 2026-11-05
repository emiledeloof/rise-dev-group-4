namespace Rise.Domain.ExtraServices;

/// <summary>
/// An additional service offered by the hotel (e.g. Breakfast, Extra Cleaning, Bicycle Rental).
/// Can be charged per night or once, and per person or per booking.
/// </summary>
public class ExtraService : Entity
{
    private string _name = string.Empty;

    /// <summary>
    /// Display name of the extra service (e.g. "Breakfast").
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private string? _description;

    /// <summary>
    /// Optional description of what the extra service provides.
    /// </summary>
    public string? Description
    {
        get => _description;
        set => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private decimal _price;

    /// <summary>
    /// Base price in EUR (must not be negative).
    /// </summary>
    public decimal Price
    {
        get => _price;
        set => _price = Guard.Against.Negative(value);
    }

    /// <summary>
    /// Billing frequency (PerNight or Once).
    /// </summary>
    public ChargeCadence Cadence { get; set; } = ChargeCadence.PerNight;

    /// <summary>
    /// If true, the price is multiplied by the number of guests (e.g. Breakfast).
    /// If false, charged per booking / fixed unit (e.g. Extra Cleaning).
    /// </summary>
    public bool IsPerPerson { get; set; }

    /// <summary>
    /// Whether this extra service is currently active and available to offer.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Parameterless constructor for Entity Framework Core.
    /// </summary>
    private ExtraService()
    {
    }

    /// <summary>
    /// Creates a new extra service with required invariants.
    /// </summary>
    public ExtraService(
        string name,
        decimal price,
        ChargeCadence cadence = ChargeCadence.PerNight,
        bool isPerPerson = false,
        string? description = null)
    {
        Name = name;
        Price = price;
        Cadence = cadence;
        IsPerPerson = isPerPerson;
        Description = description;
        IsActive = true;
    }

    /// <summary>
    /// Updates mutable fields of the extra service.
    /// </summary>
    public void Update(
        string name,
        decimal price,
        ChargeCadence cadence,
        bool isPerPerson,
        string? description)
    {
        Name = name;
        Price = price;
        Cadence = cadence;
        IsPerPerson = isPerPerson;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
