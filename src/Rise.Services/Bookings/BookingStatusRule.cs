using System.Linq.Expressions;
using Rise.Domain.Bookings;
using Rise.Shared.Bookings;

namespace Rise.Services.Bookings;

/// <summary>
/// the rule behind <see cref="BookingStatus"/>, shared by the booking list, the booking detail and the final bill
/// </summary>
public static class BookingStatusRule
{
    /// <summary>
    /// a payment that counts as paid
    /// an expression so a query can run it in SQL, <see cref="Payment.IsPaid"/> isn't mapped
    /// </summary>
    public static readonly Expression<Func<Payment, bool>> IsSettled = x => !x.IsDeleted && x.PaidAt != null;

    private static readonly Func<Payment, bool> IsSettledInMemory = IsSettled.Compile();

    /// <summary>
    /// status of a booking with its payments loaded
    /// </summary>
    public static string For(Booking booking)
        => For(booking.IsCancelled, booking.IsSettled, booking.Payments.Any(IsSettledInMemory));

    /// <summary>
    /// cancelled wins over settled, settled over paid, for a query that only selects these 3 facts
    /// </summary>
    public static string For(bool isCancelled, bool isBillSettled, bool hasSettledPayment)
    {
        if (isCancelled) return BookingStatus.Cancelled;

        // * the closed bill matters more to the admin than how much was paid
        if (isBillSettled) return BookingStatus.Settled;

        return hasSettledPayment ? BookingStatus.Paid : BookingStatus.Pending;
    }
}
