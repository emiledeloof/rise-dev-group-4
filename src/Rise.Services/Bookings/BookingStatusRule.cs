using System.Linq.Expressions;
using Rise.Domain.Bookings;
using Rise.Shared.Bookings;

namespace Rise.Services.Bookings;

/// <summary>
/// the rules behind <see cref="BookingStatus"/> and <see cref="BookingRefundStatus"/>, shared by the booking list, the booking detail and the final bill
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

    /// <summary>
    /// refund status of the cancelled part of a booking with its rooms loaded
    /// </summary>
    public static string RefundStatusFor(Booking booking)
    {
        var cancelled = booking.Rooms.Where(x => !x.IsDeleted && x.IsCancelled).ToList();
        return RefundStatusFor(cancelled.Count, cancelled.Count(x => x.RefundAmount > 0), cancelled.Count(x => x.HasOpenRefund));
    }

    /// <summary>
    /// an open refund wins, so a refund can't be forgotten behind a paid one, for a query that only selects the counts
    /// </summary>
    /// <param name="cancelledRooms">nb of cancelled rooms</param>
    /// <param name="roomsWithRefund">nb of cancelled rooms with a refund above 0</param>
    /// <param name="openRefunds">nb of those refunds not paid back yet</param>
    public static string RefundStatusFor(int cancelledRooms, int roomsWithRefund, int openRefunds)
    {
        if (cancelledRooms == 0) return BookingRefundStatus.None;
        if (openRefunds > 0) return BookingRefundStatus.RefundPending;

        return roomsWithRefund > 0 ? BookingRefundStatus.Refunded : BookingRefundStatus.NoRefund;
    }
}
