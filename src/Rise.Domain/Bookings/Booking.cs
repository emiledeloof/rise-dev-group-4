using Rise.Domain.Guests;
using Rise.Domain.Rooms;

namespace Rise.Domain.Bookings;

/// <summary>
/// A booking of one or more rooms by a <see cref="Guest"/> for a period of time.
/// </summary>
public class Booking : Entity
{
    /// <summary>
    /// Readable booking number with booking date and hotel initials, e.g. 20261007-SVM-000042.
    /// </summary>
    public string BookingNumber { get; private set; } = default!;
    public Guest Guest { get; private set; } = default!;
    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }

    private readonly List<BookingRoom> _rooms = [];
    public IReadOnlyList<BookingRoom> Rooms => _rooms.AsReadOnly();

    private readonly List<Payment> _payments = [];
    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Booking()
    {
    }

    public Booking(string bookingNumber, Guest guest, DateOnly checkIn, DateOnly checkOut)
    {
        BookingNumber = Guard.Against.NullOrWhiteSpace(bookingNumber).Trim();
        Guest = Guard.Against.Null(guest);
        ChangeDates(checkIn, checkOut);
    }

    public void ChangeDates(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in.", nameof(checkOut));

        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    /// <summary>
    /// Adds a room to the booking, the price per night is copied from the rate at booking time.
    /// </summary>
    public BookingRoom AddRoom(Room room, Rate rate)
    {
        Guard.Against.Null(room);
        Guard.Against.Null(rate);

        if (rate.Room != room)
            throw new ArgumentException("The rate does not belong to the room.", nameof(rate));

        if (_rooms.Any(x => x.Room == room))
            throw new ArgumentException("The room is already part of this booking.", nameof(room));

        var bookingRoom = new BookingRoom(this, room, rate);
        _rooms.Add(bookingRoom);
        return bookingRoom;
    }

    public Payment AddPayment(decimal amount, string? stripeCheckoutSession = null)
    {
        var payment = new Payment(this, amount, stripeCheckoutSession);
        _payments.Add(payment);
        return payment;
    }
}
