using Rise.Domain.Bookings;

namespace Rise.Services.Bookings;

/// <summary>
/// the overlap rule shared by the search, the booking creation and the matrix
/// </summary>
public static class BookingRoomQueryExtensions
{
    /// <summary>
    /// booked rooms whose stay overlaps [from, to), ie starts before to and ends after from
    /// deleted and cancelled bookings free their rooms
    /// </summary>
    public static IQueryable<BookingRoom> Overlapping(this IQueryable<BookingRoom> query, DateOnly from, DateOnly to)
    {
        return query.Where(br => !br.IsDeleted
            && !br.Booking.IsDeleted
            && br.Booking.CancelledAt == null
            && br.Booking.CheckIn < to
            && br.Booking.CheckOut > from);
    }
}
