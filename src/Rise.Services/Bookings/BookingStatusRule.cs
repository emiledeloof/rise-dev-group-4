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
        => For(booking.IsCancelled, booking.Payments.Any(IsSettledInMemory));

    /// <summary>
    /// cancelled wins over paid, for a query that only selects these 2 facts
    /// </summary>
    public static string For(bool isCancelled, bool hasSettledPayment)
    {
        if (isCancelled) return BookingStatus.Cancelled;

        return hasSettledPayment ? BookingStatus.Paid : BookingStatus.Pending;
    }
}
