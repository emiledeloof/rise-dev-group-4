namespace Rise.Domain.Guests;

/// <summary>
/// The address of a <see cref="Guest"/> using the <see cref="ValueObject"/> design pattern.
/// </summary>
public class GuestAddress : ValueObject
{
    public string AddressLine1 { get; } = default!;
    public string? AddressLine2 { get; }
    public string Country { get; } = default!;

    /// <summary>
    /// Entity Framework requires a default constructor.
    /// </summary>
    private GuestAddress()
    {
    }

    public GuestAddress(string addressLine1, string? addressLine2, string country)
    {
        AddressLine1 = Guard.Against.NullOrWhiteSpace(addressLine1).Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        Country = Guard.Against.NullOrWhiteSpace(country).Trim();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return AddressLine1;
        yield return AddressLine2 ?? string.Empty;
        yield return Country;
    }
}
