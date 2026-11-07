namespace Rise.Shared.Bookings;

/// <summary>
/// Derived status of a booking, shown in the admin booking list and detail.
/// Cancelled when all rooms are cancelled, it wins over Settled, Settled over Paid, Paid when at least one payment is settled, otherwise Pending.
/// A partly cancelled booking keeps its payment status, the room counts on the DTOs show the cancelled rooms.
/// </summary>
public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    /// <summary>the final bill is closed</summary>
    public const string Settled = "Settled";
}
