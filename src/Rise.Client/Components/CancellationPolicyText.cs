using Rise.Shared.Bookings;
using Rise.Shared.Rooms;

namespace Rise.Client.Components;

/// <summary>
/// cancellation policy of a rate as a sentence, used by the room card, the booking overview and the admin booking detail
/// same idea as FormatPolicy on the admin rate pricing page
/// </summary>
public static class CancellationPolicyText
{
    /// <summary>
    /// eg "Free cancellation up to 30 days before arrival, 50% refund up to 7 days before"
    /// </summary>
    /// <param name="rate">rate picked by the guest, holds the policy values</param>
    public static string Of(RateDto.Available rate)
        => rate.IsRefundable
            ? $"Free cancellation up to {rate.FullRefundDays} days before arrival, {rate.PartialRefundPercent}% refund up to {rate.PartialRefundDays} days before"
            : "Non-refundable, no refund if you cancel";

    /// <summary>
    /// same sentence for a booked room on the admin booking detail, from the policy copied at booking time
    /// </summary>
    /// <param name="room">booked room, holds the policy values agreed at booking time</param>
    public static string Of(BookingDto.AdminRoomDto room)
        => room.IsRefundable
            ? $"Free cancellation up to {room.FullRefundDays} days before arrival, {room.PartialRefundPercent}% refund up to {room.PartialRefundDays} days before"
            : "Non-refundable, no refund if the guest cancels";
}
