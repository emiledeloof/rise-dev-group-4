namespace Rise.Shared.Email;

/// <summary>
/// All data needed to render and send a booking confirmation email to the guest.
/// </summary>
public class BookingConfirmationEmail
{
    public required string GuestName { get; init; }

    /// <summary>
    /// The guest's email address (the destination of the email).
    /// </summary>
    public required string GuestEmail { get; init; }

    public required string BookingNumber { get; init; }
    public required DateOnly CheckIn { get; init; }
    public required DateOnly CheckOut { get; init; }
    public required int Nights { get; init; }
    public required int Adults { get; init; }

    /// <summary>
    /// One entry per room included in the booking.
    /// </summary>
    public required List<BookingConfirmationRoom> Rooms { get; init; }

    /// <summary>
    /// Grand total for all rooms and all nights, inclusive of VAT (the price the guest paid).
    /// </summary>
    public required decimal TotalAmount { get; init; }
}

/// <summary>
/// A single room line inside a <see cref="BookingConfirmationEmail"/>.
/// </summary>
public class BookingConfirmationRoom
{
    public required string RoomNumber { get; init; }
    public required string RoomName { get; init; }

    /// <summary>
    /// Name of the rate as it was at booking time.
    /// </summary>
    public required string RateName { get; init; }

    public required decimal PricePerNight { get; init; }

    /// <summary>
    /// Total for this room for all nights of the stay.
    /// </summary>
    public required decimal Total { get; init; }
}
