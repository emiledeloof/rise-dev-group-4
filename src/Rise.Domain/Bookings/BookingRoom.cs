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
    /// Entity Framework Core Constructor
    /// </summary>
    private BookingRoom()
    {
    }

    internal BookingRoom(Booking booking, Room room, Rate rate)
    {
        Booking = Guard.Against.Null(booking);
        Room = Guard.Against.Null(room);
        Rate = Guard.Against.Null(rate);
        PricePerNight = rate.PricePerNight;
    }
}
