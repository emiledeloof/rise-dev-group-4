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

        public bool IsActive { get; set; } = true;

        // min nb of nights a guest must book
        public int MinNights { get; set; }

        // Null bounds mean the rate has no lower or upper availability limit.
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // true for a refundable rate, false for non-refundable, shared cant see the domain enum
        public bool IsRefundable { get; set; }

        // 100 pct refund up to this nb of days before arrival
        public int FullRefundDays { get; set; }

        // partial refund up to this nb of days before arrival
        public int PartialRefundDays { get; set; }

        // pct refunded in the partial period, eg 50
        public int PartialRefundPercent { get; set; }

        /// <summary>
        /// rules checked on the client for feedback, then again on the server by fastendpoints
        /// </summary>
        public class Validator : AbstractValidator<Edit>
        {
            public Validator()
            {
                // a real rate is always picked
                RuleFor(r => r.RateId).GreaterThan(0);

                // * max 2 decimals, same precision as the db column
                RuleFor(x => x.PricePerNight)
                    .GreaterThan(0)
                    .WithMessage("The price per night must be more than 0.")
                    .PrecisionScale(18, 2, true)
                    .WithMessage("The price can have at most 2 decimals.");

                RuleFor(x => x.MinNights)
                    .GreaterThan(0)
                    .WithMessage("A stay must be at least 1 night.");

                RuleFor(x => x.EndDate)
                    .Must((request, endDate) =>
                        request.StartDate is null ||
                        endDate is null ||
                        request.StartDate.Value < endDate.Value)
                    .WithMessage("Start date must be before end date.");

                // ! refund rules only for a refundable rate, the values are ignored otherwise
                RuleFor(x => x.PartialRefundPercent)
                    .InclusiveBetween(0, 100)
                    .WithMessage("The partial refund must be between 0 and 100%.")
                    .When(x => x.IsRefundable);

                // * eg 30 days at 100 pct then 7 days at 50 pct, never the other way round
                RuleFor(x => x.FullRefundDays)
                    .GreaterThan(x => x.PartialRefundDays)
                    .WithMessage("The full refund period must be longer than the partial one.")
                    .When(x => x.IsRefundable);

                RuleFor(x => x.PartialRefundDays)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("The number of days can't be negative.")
                    .When(x => x.IsRefundable);
            }
        }
    }
}
