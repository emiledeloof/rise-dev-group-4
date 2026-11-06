namespace Rise.Domain.Rooms;

/// <summary>
/// The price per night of a <see cref="Room"/> (e.g. Standard), created via <see cref="Room.AddRate"/>.
/// </summary>
public class Rate : Entity
{
    public Room Room { get; private set; } = default!;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    private decimal _pricePerNight;
    public decimal PricePerNight
    {
        get => _pricePerNight;
        set => _pricePerNight = Guard.Against.NegativeOrZero(value);
    }

    public bool IsActive { get; set; } = true;

    // min nb of nights a guest must book with this rate, eg 1
    private int _minNights = 1;
    public int MinNights
    {
        get => _minNights;
        set => _minNights = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>
    /// refundable by default, the standard policy of the client
    /// </summary>
    public CancellationPolicy CancellationPolicy { get; set; } = CancellationPolicy.Refundable;

    /// <summary>
    /// 100 pct refund when cancelled at least this nb of days before arrival, only for a refundable rate
    /// </summary>
    private int _fullRefundDays = 30;
    public int FullRefundDays
    {
        get => _fullRefundDays;
        set => _fullRefundDays = Guard.Against.Negative(value);
    }

    /// <summary>
    /// partial refund when cancelled at least this nb of days before arrival, nothing after
    /// </summary>
    private int _partialRefundDays = 7;
    public int PartialRefundDays
    {
        get => _partialRefundDays;
        set => _partialRefundDays = Guard.Against.Negative(value);
    }

    /// <summary>
    /// pct refunded in the partial period, between 0 and 100
    /// </summary>
    private int _partialRefundPercent = 50;
    public int PartialRefundPercent
    {
        get => _partialRefundPercent;
        set => _partialRefundPercent = Guard.Against.OutOfRange(value, nameof(value), 0, 100);
    }

    /// <summary>
    /// conditions of the rate shown to the guest, e.g. no refund after booking
    /// </summary>
    public string? Description { get; set; }

    // Either bound can be null for an open-ended availability period.
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Rate()
    {
    }

    internal Rate(Room room, string name, decimal pricePerNight, string? description = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        Room = Guard.Against.Null(room);
        Name = name;
        PricePerNight = pricePerNight;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
    }
}
