namespace Rise.Shared.Rooms;

/// <summary>
/// rate data sent from the server to the client, same idea as RoomDto
/// </summary>
public static class RateDto
{
    /// <summary>
    /// one rate of a free room, eg non-refundable, with its price for the searched stay
    /// </summary>
    public class Available
    {
        // id of the rate in the db, needed later to book this exact rate
        public required int Id { get; set; }

        // name shown to the guest, eg Non-refundable
        public required string Name { get; set; }

        // conditions of the rate, eg no refund after booking, a rate can have none
        public string? Description { get; set; }

        // price for 1 night with this rate
        public required decimal PricePerNight { get; set; }

        // price for the whole stay with this rate, from PriceCalculator
        public required decimal TotalPrice { get; set; }

        // true for a refundable rate, shared cant see the domain enum
        public required bool IsRefundable { get; set; }

        // 100 pct refund up to this nb of days before arrival
        public required int FullRefundDays { get; set; }

        // partial refund up to this nb of days before arrival
        public required int PartialRefundDays { get; set; }

        // pct refunded in the partial period, eg 50
        public required int PartialRefundPercent { get; set; }
    }

    /// <summary>
    /// one row of the admin rates page, with what the edit form needs
    /// </summary>
    public class Index
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string RoomName { get; set; }
        public required decimal PricePerNight { get; set; }
        public string? Description { get; set; }

        // min nb of nights a guest must book
        public required int MinNights { get; set; }

        // true for a refundable rate, shared cant see the domain enum
        public required bool IsRefundable { get; set; }

        // 100 pct refund up to this nb of days before arrival
        public required int FullRefundDays { get; set; }

        // partial refund up to this nb of days before arrival
        public required int PartialRefundDays { get; set; }

        // pct refunded in the partial period, eg 50
        public required int PartialRefundPercent { get; set; }
    }
}
