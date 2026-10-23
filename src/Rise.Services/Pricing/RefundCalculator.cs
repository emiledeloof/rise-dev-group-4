using Rise.Domain.Bookings;
using Rise.Domain.Rooms;

namespace Rise.Services.Pricing;

/// <summary>
/// refund rules when a guest cancels, same idea as <see cref="PriceCalculator"/>
/// ! only gives the pct, the refund itself is always done by hand by the admin
/// </summary>
public static class RefundCalculator
{
    /// <summary>
    /// pct of the room price given back, eg 100, 50 or 0
    /// days are counted on dates only, so a guest can cancel until midnight of the last day
    /// </summary>
    /// <param name="bookingRoom">booked room, holds a copy of the policy of the rate at booking time</param>
    /// <param name="checkIn">arrival date of the booking</param>
    /// <param name="cancelledOn">date the guest cancels</param>
    public static int RefundPercent(BookingRoom bookingRoom, DateOnly checkIn, DateOnly cancelledOn)
    {
        // * non refundable rate, nothing back whatever the date
        if (bookingRoom.CancellationPolicy is CancellationPolicy.NonRefundable) return 0;

        // ! check in minus cancel date, the arrival is later so the result is positive
        // eg cancelled on 1 oct for a check in on 31 oct gives 30
        var daysBeforeArrival = checkIn.DayNumber - cancelledOn.DayNumber;

        // biggest refund first, the 1st rule that matches wins
        if (daysBeforeArrival >= bookingRoom.FullRefundDays) return 100;
        if (daysBeforeArrival >= bookingRoom.PartialRefundDays) return bookingRoom.PartialRefundPercent;

        // too late, nothing back
        return 0;
    }
}
