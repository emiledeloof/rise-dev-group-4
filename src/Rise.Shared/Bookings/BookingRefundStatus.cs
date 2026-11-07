namespace Rise.Shared.Bookings;

/// <summary>
/// Derived status of the cancelled part of a booking, shown next to <see cref="BookingStatus"/>.
/// Splitting both keeps a partly cancelled booking clear: the ongoing part keeps its payment status, the cancelled part has its own refund status.
/// </summary>
public static class BookingRefundStatus
{
    /// <summary>
    /// No room is cancelled, there is nothing to refund.
    /// </summary>
    public const string None = "None";

    /// <summary>
    /// Rooms are cancelled, but no refund was decided for any of them.
    /// </summary>
    public const string NoRefund = "NoRefund";

    /// <summary>
    /// At least one decided refund is not paid back yet, the admin still has to pay it.
    /// </summary>
    public const string RefundPending = "RefundPending";

    /// <summary>
    /// All decided refunds are paid back.
    /// </summary>
    public const string Refunded = "Refunded";
}
