namespace Rise.Shared.Billing;

public static partial class BillingRequest
{
    /// <summary>
    /// A free line on the bill for something the guest used, eg minibar or an excursion.
    /// </summary>
    public class AddLine
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/bill/lines
        /// </summary>
        public int BookingId { get; set; }

        public string Description { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Price of 1 unit including VAT, like the room prices.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// One of <see cref="VatRates.Presets"/>.
        /// </summary>
        public decimal VatPercentage { get; set; } = VatRates.Default;

        public class Validator : AbstractValidator<AddLine>
        {
            public Validator()
            {
                RuleFor(x => x.Description)
                    .NotEmpty()
                    .WithMessage("Please describe what the guest used.")
                    .MaximumLength(200);
                RuleFor(x => x.Quantity)
                    .InclusiveBetween(1, 999)
                    .WithMessage("The quantity must be between 1 and 999.");
                RuleFor(x => x.UnitPrice)
                    .GreaterThan(0)
                    .WithMessage("The price must be more than €0.")
                    .LessThanOrEqualTo(100_000)
                    .WithMessage("The price can be at most €100,000.")
                    .PrecisionScale(8, 2, true)
                    .WithMessage("The price can have at most 2 decimals.");
                RuleFor(x => x.VatPercentage)
                    .Must(x => VatRates.Presets.Contains(x))
                    .WithMessage("Please pick one of the VAT rates.");
            }
        }
    }

    /// <summary>
    /// Changes a free line, the same fields as <see cref="AddLine"/> plus the line.
    /// </summary>
    public class EditLine : AddLine
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/bill/lines/{LineId}
        /// </summary>
        public int LineId { get; set; }

        public new class Validator : AbstractValidator<EditLine>
        {
            public Validator()
            {
                Include(new AddLine.Validator());
            }
        }
    }

    public class DeleteLine
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/bill/lines/{LineId}
        /// </summary>
        public int BookingId { get; set; }

        public int LineId { get; set; }
    }

    /// <summary>
    /// A payment the guest made on site, eg cash at the desk, it counts as paid right away.
    /// The service refuses an amount above the outstanding balance.
    /// </summary>
    public class RecordPayment
    {
        public const int ReferenceMaxLength = 100;

        /// <summary>
        /// From the route, /api/bookings/{BookingId}/bill/payments
        /// </summary>
        public int BookingId { get; set; }

        public decimal Amount { get; set; }

        /// <summary>
        /// One of <see cref="PaymentMethods.OnSite"/>.
        /// </summary>
        public string Method { get; set; } = PaymentMethods.Cash;

        /// <summary>
        /// Optional, eg the receipt or terminal slip number.
        /// </summary>
        public string? Reference { get; set; }

        public class Validator : AbstractValidator<RecordPayment>
        {
            public Validator()
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0)
                    .WithMessage("The amount must be more than €0.")
                    .LessThanOrEqualTo(100_000)
                    .WithMessage("The amount can be at most €100,000.")
                    .PrecisionScale(8, 2, true)
                    .WithMessage("The amount can have at most 2 decimals.");
                RuleFor(x => x.Method)
                    .Must(PaymentMethods.IsOnSite)
                    .WithMessage("Please pick how the guest paid.");
                RuleFor(x => x.Reference)
                    .MaximumLength(ReferenceMaxLength);
            }
        }
    }

    /// <summary>
    /// Removes an on-site payment recorded by mistake, a Stripe payment can't be deleted.
    /// </summary>
    public class DeletePayment
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/bill/payments/{PaymentId}
        /// </summary>
        public int BookingId { get; set; }

        public int PaymentId { get; set; }
    }
}
