using Rise.Domain.Bookings;

namespace Rise.Services.Bookings;

/// <summary>
/// the overlap rule shared by the search, the booking creation and the matrix
/// </summary>
public static class BookingRoomQueryExtensions
{
    /// <summary>
    /// booked rooms whose stay overlaps [from, to), ie starts before to and ends after from
    /// deleted bookings and cancelled rooms free their rooms, the other rooms of the booking stay taken
    /// </summary>
    public static IQueryable<BookingRoom> Overlapping(this IQueryable<BookingRoom> query, DateOnly from, DateOnly to)
    {
        return query.Where(br => !br.IsDeleted
            && !br.Booking.IsDeleted
            && br.CancelledAt == null
            && br.Booking.CheckIn < to
            && br.Booking.CheckOut > from);
    }
}
