using Rise.Shared.Rooms;

namespace Rise.Client.Components;

/// <summary>
/// cancellation policy of a rate as a sentence for the guest, used by the room card and the booking overview
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
}
