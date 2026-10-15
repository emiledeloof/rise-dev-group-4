using Rise.Shared.Bookings;
using Rise.Shared.Guests;

namespace Rise.Client.Bookings;

/// <summary>
/// the booking the guest is putting together, kept in the browser until "Pay now"
/// filled step by step by the results page, the guest form and read by the overview
/// registered as scoped in Program.cs, in wasm that means one per browser tab
/// it lives in memory only, a page refresh empties it and the pages send the guest back to the search
/// </summary>
public class BookingDraft
{
    /// <summary>
    /// arrival date of the stay
    /// </summary>
    public DateOnly? CheckIn { get; private set; }

    /// <summary>
    /// departure date of the stay
    /// </summary>
    public DateOnly? CheckOut { get; private set; }

    /// <summary>
    /// rooms the guest picked with their rate, prices are only for display, the server calculates them again
    /// </summary>
    public IReadOnlyList<DraftRoom> Rooms { get; private set; } = [];

    /// <summary>
    /// details entered on the guest form, null until the form is submitted
    /// </summary>
    public GuestRequest.Create? Guest { get; private set; }

    /// <summary>
    /// nb of nights of the stay, 0 when the dates are missing
    /// </summary>
    public int Nights => CheckIn is { } checkIn && CheckOut is { } checkOut ? checkOut.DayNumber - checkIn.DayNumber : 0;

    /// <summary>
    /// total of all the picked rooms for the whole stay
    /// </summary>
    public decimal Total => Rooms.Sum(r => r.TotalPrice);

    /// <summary>
    /// true when the dates and at least one room are picked
    /// </summary>
    public bool HasRooms => CheckIn.HasValue && CheckOut.HasValue && Rooms.Count > 0;

    /// <summary>
    /// true when everything needed for the booking is there
    /// </summary>
    public bool IsComplete => HasRooms && Guest is not null;

    /// <summary>
    /// stores the stay and the picked rooms, called by the continue btn of the results page
    /// </summary>
    public void SetRooms(DateOnly checkIn, DateOnly checkOut, IEnumerable<DraftRoom> rooms)
    {
        CheckIn = checkIn;
        CheckOut = checkOut;
        Rooms = rooms.ToList();
    }

    /// <summary>
    /// stores the guest details, called by the guest form
    /// </summary>
    public void SetGuest(GuestRequest.Create guest)
    {
        Guest = guest;
    }

    /// <summary>
    /// forgets everything, called once the booking is saved
    /// </summary>
    public void Clear()
    {
        CheckIn = null;
        CheckOut = null;
        Rooms = [];
        Guest = null;
    }

    /// <summary>
    /// the request sent to the api when the guest clicks "Pay now"
    /// </summary>
    public BookingRequest.Create ToCreateRequest()
    {
        if (!IsComplete)
            throw new InvalidOperationException("The booking draft is not complete.");

        return new BookingRequest.Create
        {
            CheckIn = CheckIn!.Value,
            CheckOut = CheckOut!.Value,
            Guest = Guest!,
            Rooms = Rooms.Select(r => new BookingRequest.Create.RoomSelection { RoomId = r.RoomId, RateId = r.RateId }).ToList(),
        };
    }
}

/// <summary>
/// one picked room of the draft with the rate the guest chose
/// </summary>
public record DraftRoom(
    int RoomId,
    string RoomName,
    int RateId,
    string RateName,
    string? RateDescription,
    decimal PricePerNight,
    decimal TotalPrice);
