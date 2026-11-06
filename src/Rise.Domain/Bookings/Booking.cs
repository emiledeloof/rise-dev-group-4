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

    /// <summary>
    /// Nb of guests of the stay, chosen in the search, at least 1.
    /// </summary>
    public int Adults { get; private set; }

    private readonly List<BookingRoom> _rooms = [];
    public IReadOnlyList<BookingRoom> Rooms => _rooms.AsReadOnly();

    private readonly List<Payment> _payments = [];
    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    /// <summary>
    /// Free lines the admin adds to the bill for what the guest used, eg minibar or an excursion.
    /// </summary>
    private readonly List<BillLine> _billLines = [];
    public IReadOnlyList<BillLine> BillLines => _billLines.AsReadOnly();

    /// <summary>
    /// Nb of nights of the stay.
    /// </summary>
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

    /// <summary>
    /// Rooms that are still part of the stay, the rooms must be loaded.
    /// </summary>
    public IReadOnlyList<BookingRoom> ActiveRooms => _rooms.Where(x => !x.IsDeleted && !x.IsCancelled).ToList();

    /// <summary>
    /// True when all rooms are cancelled, derived from the rooms so it can't get out of sync, the rooms must be loaded.
    /// A cancelled booking keeps its rooms, prices and payments for the admin.
    /// </summary>
    public bool IsCancelled => _rooms.Any(x => !x.IsDeleted) && _rooms.Where(x => !x.IsDeleted).All(x => x.IsCancelled);

    /// <summary>
    /// True when some but not all rooms are cancelled, the rooms must be loaded.
    /// </summary>
    public bool IsPartiallyCancelled => !IsCancelled && _rooms.Any(x => !x.IsDeleted && x.IsCancelled);

    /// <summary>
    /// Price of the rooms that are still part of the stay, what a new checkout charges, the rooms must be loaded.
    /// </summary>
    public decimal ActiveAmount => ActiveRooms.Sum(x => x.TotalPrice);

    /// <summary>
    /// Sum of the refunds recorded on the cancelled rooms, the rooms must be loaded.
    /// </summary>
    public decimal RefundedAmount => _rooms.Where(x => !x.IsDeleted).Sum(x => x.RefundAmount ?? 0m);

    /// <summary>
    /// Sum of the settled payments, the most that can be refunded.
    /// </summary>
    public decimal PaidAmount => _payments.Where(x => x.IsPaid && !x.IsDeleted).Sum(x => x.Amount);

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Booking()
    {
    }

    public Booking(string bookingNumber, Guest guest, DateOnly checkIn, DateOnly checkOut, int adults)
    {
        BookingNumber = Guard.Against.NullOrWhiteSpace(bookingNumber).Trim();
        Guest = Guard.Against.Null(guest);
        Adults = Guard.Against.NegativeOrZero(adults);
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
    /// <param name="vatPercentage">VAT pct included in the price per night, 0 when VAT is off</param>
    public BookingRoom AddRoom(Room room, Rate rate, decimal? pricePerNight = null, decimal vatPercentage = 0m)
    {
        Guard.Against.Null(room);
        Guard.Against.Null(rate);

        if (rate.Room != room)
            throw new ArgumentException("The rate does not belong to the room.", nameof(rate));

        if (_rooms.Any(x => x.Room == room))
            throw new ArgumentException("The room is already part of this booking.", nameof(room));

        var bookingRoom = new BookingRoom(this, room, rate, pricePerNight ?? rate.PricePerNight, vatPercentage);
        _rooms.Add(bookingRoom);
        return bookingRoom;
    }

    /// <summary>
    /// Cancels one or more rooms and records the refund of each, cancelling all rooms cancels the booking.
    /// Everything is checked first, so a refused cancellation changes nothing.
    /// The rooms with their <see cref="Room"/> and the payments must be loaded.
    /// BookingService.CancelAsync checks the same rules first to return a Result, keep both in sync.
    /// </summary>
    /// <param name="rooms">rooms to cancel with their refund</param>
    /// <param name="cancelledAt">moment of the cancellation</param>
    /// <param name="reason">why, stored on each room of this cancellation</param>
    /// <param name="adults">new nb of guests of the remaining rooms, null keeps the current nb</param>
    public void CancelRooms(IReadOnlyCollection<RoomCancellation> rooms, DateTime cancelledAt, string reason, int? adults = null)
    {
        Guard.Against.Null(rooms);
        Guard.Against.NullOrWhiteSpace(reason);

        if (rooms.Count == 0)
            throw new ArgumentException("Select at least one room to cancel.", nameof(rooms));

        if (rooms.Select(x => x.Room).Distinct().Count() != rooms.Count)
            throw new ArgumentException("A room can only be cancelled once.", nameof(rooms));

        foreach (var (room, refund) in rooms)
        {
            if (!_rooms.Contains(room) || room.IsDeleted)
                throw new ArgumentException("The room is not part of this booking.", nameof(rooms));

            if (room.IsCancelled)
                throw new InvalidOperationException("The room is already cancelled.");

            Guard.Against.Negative(refund);

            if (refund > room.TotalPrice)
                throw new ArgumentException("The refund can't be more than the price of the room.", nameof(rooms));
        }

        if (RefundedAmount + rooms.Sum(x => x.RefundAmount) > PaidAmount)
            throw new ArgumentException("The refunds can't be more than the amount paid.", nameof(rooms));

        var remaining = ActiveRooms.Where(x => rooms.All(c => c.Room != x)).ToList();

        // ! with no rooms left the nb of guests is only history, nobody has to fit anywhere anymore
        if (remaining.Count > 0)
        {
            var guests = Guard.Against.NegativeOrZero(adults ?? Adults);

            if (guests > remaining.Sum(x => x.Room.MaxGuests))
                throw new ArgumentException("The guests don't fit in the remaining rooms.", nameof(adults));

            Adults = guests;
        }

        var trimmedReason = reason.Trim();
        foreach (var (room, refund) in rooms)
            room.Cancel(cancelledAt, trimmedReason, refund);
    }

    public Payment AddPayment(decimal amount, string? stripeCheckoutSession = null)
    {
        var payment = new Payment(this, amount, stripeCheckoutSession);
        _payments.Add(payment);
        return payment;
    }

    /// <summary>
    /// Adds a free line to the bill, the bill of a fully cancelled booking can't be changed.
    /// The rooms must be loaded, <see cref="IsCancelled"/> is derived from them.
    /// </summary>
    public BillLine AddBillLine(string description, int quantity, decimal unitPrice, decimal vatPercentage)
    {
        if (IsCancelled)
            throw new InvalidOperationException("The bill of a cancelled booking can't be changed.");

        var line = new BillLine(this, description, quantity, unitPrice, vatPercentage);
        _billLines.Add(line);
        return line;
    }
}
