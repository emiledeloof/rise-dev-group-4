namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// Cancels a booking, sent by the admin from the booking detail page.
    /// The refund is only recorded, the admin pays it back by hand, the website never refunds via Stripe.
    /// </summary>
    public sealed class Cancel
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/cancel
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Why the booking is cancelled, kept on the booking for later.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Refund the admin decided on, between 0 and the amount paid, checked again by the server.
        /// </summary>
        public decimal RefundAmount { get; set; }

        public class Validator : AbstractValidator<Cancel>
        {
            public Validator()
            {
                RuleFor(x => x.Reason)
                    .NotEmpty()
                    .WithMessage("Please enter a reason for the cancellation.")
                    .MaximumLength(500);
                RuleFor(x => x.RefundAmount)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("The refund can't be negative.");
            }
        }
    }
}

public static partial class BookingResponse
{
    public sealed class Cancel
    {
        public required int BookingId { get; init; }
        public required string BookingNumber { get; init; }
        public required DateTime CancelledAt { get; init; }
        public required decimal RefundAmount { get; init; }
    }
}
