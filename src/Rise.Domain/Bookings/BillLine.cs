namespace Rise.Domain.Bookings;

/// <summary>
/// A free line on the bill of a <see cref="Booking"/> for something the guest used, eg minibar or an excursion,
/// created via <see cref="Booking.AddBillLine"/>. Not linked to a service catalogue, the admin types it in.
/// </summary>
public class BillLine : Entity
{
    public const int DescriptionMaxLength = 200;

    public Booking Booking { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }

    /// <summary>
    /// Price of 1 unit including VAT, like the room prices.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// VAT pct included in <see cref="UnitPrice"/>, eg 22.
    /// </summary>
    public decimal VatPercentage { get; private set; }

    public decimal Total => Quantity * UnitPrice;

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private BillLine()
    {
    }

    internal BillLine(Booking booking, string description, int quantity, decimal unitPrice, decimal vatPercentage)
    {
        Booking = Guard.Against.Null(booking);
        Update(description, quantity, unitPrice, vatPercentage);
    }

    public void Update(string description, int quantity, decimal unitPrice, decimal vatPercentage)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.StringTooLong(description.Trim(), DescriptionMaxLength, nameof(description));

        Description = description.Trim();
        Quantity = Guard.Against.NegativeOrZero(quantity);
        UnitPrice = Guard.Against.NegativeOrZero(unitPrice);
        VatPercentage = Guard.Against.OutOfRange(vatPercentage, nameof(vatPercentage), 0m, 100m);
    }
}
