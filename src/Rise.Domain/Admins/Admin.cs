namespace Rise.Domain.Admins;

/// <summary>
/// An admin manages the hotel (rooms, rates, bookings).
/// This is a domain entity and seperated from the <see cref="IdentityUser"/> the link is made via the <see cref="AccountId"/>.
/// The credentials (email, password hash) are stored by ASP.NET Identity, not in this entity.
/// </summary>
public class Admin : Entity
{
    /// <summary>
    /// Link to the <see cref="IdentityUser"/> account, so an Admin HAS A Account and not IS A <see cref="IdentityUser"/>.
    /// </summary>
    public string AccountId { get; private set; } = default!;

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Admin()
    {
    }

    public Admin(string accountId)
    {
        AccountId = Guard.Against.NullOrEmpty(accountId);
    }
}
