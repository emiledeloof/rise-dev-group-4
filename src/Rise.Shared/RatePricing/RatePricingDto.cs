namespace Rise.Shared.RatePricing;

/// <summary>
/// pricing data of a rate for the admin pricing page, only the fields the page needs
/// </summary>
public static class RatePricingDto
{
    /// <summary>
    /// one row of the pricing page, a rate with its room and its current price
    /// </summary>
    public class Index
    {
        // id of the rate in the db
        public required int RateId { get; set; }

        // name of the room the rate belongs to, eg Deluxe Room
        public required string RoomName { get; set; }

        // name of the rate, eg Standard
        public required string RateName { get; set; }

        // current price for 1 night in eur
        public required decimal PricePerNight { get; set; }

        // min nb of nights a guest must book
        public required int MinNights { get; set; }

        // true for a refundable rate, false for non-refundable, shared cant see the domain enum
        public required bool IsRefundable { get; set; }

        // 100 pct refund up to this nb of days before arrival
        public required int FullRefundDays { get; set; }

        // partial refund up to this nb of days before arrival
        public required int PartialRefundDays { get; set; }

        // pct refunded in the partial period, eg 50
        public required int PartialRefundPercent { get; set; }
    }
}
