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
}
