namespace Rise.Shared.Bookings;

/// <summary>
/// Derived status of a booking, shown in the admin booking list and detail.
/// Cancelled wins over Paid, Paid when at least one payment is settled, otherwise Pending.
/// </summary>
public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
}
