using Rise.Domain.Bookings;

namespace Rise.Domain.Guests;

/// <summary>
/// A guest is a person who books one or more rooms in the hotel.
/// </summary>
public class Guest : Entity
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => _email = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private string _phone = string.Empty;
    public string Phone
    {
        get => _phone;
        set => _phone = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    /// <summary>
    /// The address is immutable and owned by the <see cref="Guest"/>. If you want to change the address, create a new <see cref="GuestAddress"/>.
    /// </summary>
    public GuestAddress Address { get; private set; } = default!;

    private readonly List<Booking> _bookings = [];
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Guest()
    {
    }

    public Guest(string name, string email, string phone, GuestAddress address)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Address = Guard.Against.Null(address);
    }

    public void ChangeAddress(GuestAddress address)
    {
        Address = Guard.Against.Null(address);
    }
}
