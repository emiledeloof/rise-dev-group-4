namespace Rise.Shared.RatePricing;

/// <summary>
/// requests sent by the admin pricing page
/// </summary>
public static partial class RatePricingRequest
{
    /// <summary>
    /// new price of one rate, sent when the admin clicks save
    /// </summary>
    public class Edit
    {
        // id of the rate to change, int
        public int RateId { get; set; }

        // new price for 1 night in eur
        public decimal PricePerNight { get; set; }

        // min nb of nights a guest must book
        public int MinNights { get; set; }

        /// <summary>
        /// rules checked on the client for feedback, then again on the server by fastendpoints
        /// </summary>
        public class Validator : AbstractValidator<Edit>
        {
            public Validator()
            {
                // rate id must be more than 0
                RuleFor(r => r.RateId).GreaterThan(0);

                RuleFor(x => x.PricePerNight)
                // price must be more than 0, with a clear msg for the admin
                    .GreaterThan(0)
                    .WithMessage("The price per night must be more than 0.")
                // price max 2 decimals, PrecisionScale(18, 2, true), same precision as the db
                    .PrecisionScale(18, 2, true)
                    .WithMessage("The price can have at most 2 decimals.");

                RuleFor(x => x.MinNights)
                    .GreaterThan(0)
                    .WithMessage("A stay must be at least 1 night.");
            }
        }
    }
}
