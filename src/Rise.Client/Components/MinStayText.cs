using Rise.Shared.Rooms;

namespace Rise.Client.Components;

/// <summary>
/// min nb of nights of a rate as a sentence for the guest, used by the room card
/// same idea as CancellationPolicyText
/// </summary>
public static class MinStayText
{
    /// <summary>
    /// true when the stay is long enough for the rate, same check as the BookingService
    /// </summary>
    /// <param name="rate">rate of a free room</param>
    /// <param name="nights">nb of nights of the searched stay</param>
    public static bool IsBookable(RateDto.Available rate, int nights) => rate.MinNights <= nights;

    /// <summary>
    /// eg "Minimum stay 2 nights", or "Requires a stay of at least 5 nights" when the stay is too short
    /// null when the rate has no minimum, ie 1 night, so the card shows no line
    /// </summary>
    /// <param name="rate">rate of a free room, holds the min nb of nights</param>
    /// <param name="nights">nb of nights of the searched stay</param>
    public static string? Of(RateDto.Available rate, int nights)
    {
        if (!IsBookable(rate, nights))
            return $"Requires a stay of at least {rate.MinNights} nights";

        return rate.MinNights > 1 ? $"Minimum stay {rate.MinNights} nights" : null;
    }
}
