using Rise.Domain.Rooms;

namespace Rise.Domain.Bookings;

/// <summary>
/// A room that is part of a <see cref="Booking"/>, created via <see cref="Booking.AddRoom"/>.
/// </summary>
public class BookingRoom : Entity
{
    public Booking Booking { get; private set; } = default!;
    public Room Room { get; private set; } = default!;
    public Rate Rate { get; private set; } = default!;

    /// <summary>
    /// Copied from the <see cref="Rate"/> at booking time, so later price changes don't affect existing bookings.
    /// </summary>
    public decimal PricePerNight { get; private set; }

    /// <summary>
    /// copy of <see cref="Rate.Name"/> at booking time, a renamed rate doesn't change the booked rate
    /// </summary>
    public string RateName { get; private set; } = default!;

    /// <summary>
    /// copied from the <see cref="Rate"/> at booking time, the admin list of cancellations needs the policy valid when the guest booked
    /// </summary>
    public CancellationPolicy CancellationPolicy { get; private set; }

    /// <summary>
    /// copy of <see cref="Rate.FullRefundDays"/> at booking time
    /// </summary>
    public int FullRefundDays { get; private set; }

    /// <summary>
    /// copy of <see cref="Rate.PartialRefundDays"/> at booking time
    /// </summary>
    public int PartialRefundDays { get; private set; }

    /// <summary>
    /// copy of <see cref="Rate.PartialRefundPercent"/> at booking time
    /// </summary>
    public int PartialRefundPercent { get; private set; }

    /// <summary>
    /// Set by <see cref="Booking.CancelRooms"/>, a cancelled room keeps its price and policy for the admin and frees the room.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Refund decided on when cancelling this room, the website never refunds it, the admin does it by hand.
    /// </summary>
    public decimal? RefundAmount { get; private set; }

    public bool IsCancelled => CancelledAt is not null;

    /// <summary>
    /// Day the admin paid the refund back, set by <see cref="Booking.RecordRefundPayment"/>, null while it is still open.
    /// </summary>
    public DateOnly? RefundPaidOn { get; private set; }

    /// <summary>
    /// What was actually paid back, can differ from <see cref="RefundAmount"/> when the admin agreed on another amount.
    /// </summary>
    public decimal? RefundPaidAmount { get; private set; }

    /// <summary>
    /// Optional note on the refund payment, eg the Stripe refund id or "bank transfer".
    /// </summary>
    public string? RefundNote { get; private set; }

    public bool IsRefundPaid => RefundPaidOn is not null;

    /// <summary>
    /// True when a refund was decided for this cancelled room but not paid back yet.
    /// </summary>
    public bool HasOpenRefund => IsCancelled && RefundAmount > 0 && !IsRefundPaid;

    /// <summary>
    /// The refund that counts: the amount paid back once paid, otherwise the decided amount, 0 for a room that isn't cancelled.
    /// </summary>
    public decimal EffectiveRefund => RefundPaidAmount ?? RefundAmount ?? 0m;

    /// <summary>
    /// Price of this room for the whole stay, the most that can be refunded for it.
    /// </summary>
    public decimal TotalPrice => PricePerNight * Booking.Nights;

    /// <summary>
    /// VAT pct included in <see cref="PricePerNight"/> at booking time, 0 when VAT was off.
    /// Kept per room so the bill can show the VAT even after the VAT setting changes.
    /// </summary>
    public decimal VatPercentage { get; private set; }

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private BookingRoom()
    {
    }

    internal BookingRoom(Booking booking, Room room, Rate rate, decimal pricePerNight, decimal vatPercentage)
    {
        Booking = Guard.Against.Null(booking);
        Room = Guard.Against.Null(room);
        Rate = Guard.Against.Null(rate);
        PricePerNight = Guard.Against.NegativeOrZero(pricePerNight);
        VatPercentage = Guard.Against.OutOfRange(vatPercentage, nameof(vatPercentage), 0m, 100m);
        RateName = rate.Name;
        // * snapshot of the cancellation policy, later changes on the rate dont touch this booking
        CancellationPolicy = rate.CancellationPolicy;
        FullRefundDays = rate.FullRefundDays;
        PartialRefundDays = rate.PartialRefundDays;
        PartialRefundPercent = rate.PartialRefundPercent;
    }

    /// <summary>
    /// Only called by <see cref="Booking.CancelRooms"/>, which checks the rules over all rooms first.
    /// </summary>
    internal void Cancel(DateTime cancelledAt, string reason, decimal refundAmount)
    {
        CancelledAt = cancelledAt;
        CancellationReason = reason;
        RefundAmount = refundAmount;
    }

    /// <summary>
    /// Only called by <see cref="Booking.RecordRefundPayment"/>, which checks the rules first.
    /// </summary>
    internal void MarkRefundPaid(DateOnly paidOn, decimal amount, string? note)
    {
        RefundPaidOn = paidOn;
        RefundPaidAmount = amount;
        RefundNote = note;
    }

    /// <summary>
    /// Only called by <see cref="Booking.UndoRefundPayment"/>, the refund is open again.
    /// </summary>
    internal void UndoRefundPaid()
    {
        RefundPaidOn = null;
        RefundPaidAmount = null;
        RefundNote = null;
    }
}
