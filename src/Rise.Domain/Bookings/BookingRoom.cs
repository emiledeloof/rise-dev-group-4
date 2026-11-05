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
    /// Price of this room for the whole stay, the most that can be refunded for it.
    /// </summary>
    public decimal TotalPrice => PricePerNight * Booking.Nights;

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private BookingRoom()
    {
    }

    internal BookingRoom(Booking booking, Room room, Rate rate, decimal pricePerNight)
    {
        Booking = Guard.Against.Null(booking);
        Room = Guard.Against.Null(room);
        Rate = Guard.Against.Null(rate);
        PricePerNight = Guard.Against.NegativeOrZero(pricePerNight);
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
}
