using Rise.Shared.Common;

namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// A page of the admin booking list.
    /// </summary>
    public sealed class Index : QueryRequest.SkipTake
    {
        /// <summary>
        /// One of <see cref="BookingStatusFilters"/>, empty shows all bookings.
        /// </summary>
        public string? Status { get; set; }

        public override string ToString()
            => $"{base.ToString()}&Status={Uri.EscapeDataString(Status ?? "")}";

        public class Validator : AbstractValidator<Index>
        {
            public Validator()
            {
                RuleFor(x => x.Status)
                    .Must(x => string.IsNullOrEmpty(x) || BookingStatusFilters.Values.Contains(x))
                    .WithMessage("Please choose a valid status filter.");
            }
        }
    }
}

/// <summary>
/// values of the status filter on the admin booking list, they cover the <see cref="BookingStatus"/> of the ongoing part
/// and the <see cref="BookingRefundStatus"/> of the cancelled part
/// </summary>
public static class BookingStatusFilters
{
    public const string All = "All";

    /// <summary>
    /// Bookings that still have booked rooms, including partly cancelled bookings.
    /// </summary>
    public const string NotCancelled = "NotCancelled";

    /// <summary>
    /// Bookings with booked rooms and no settled payment.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Bookings with booked rooms and a settled payment.
    /// </summary>
    public const string Paid = "Paid";

    /// <summary>
    /// Bookings of which some, but not all, rooms are cancelled.
    /// </summary>
    public const string PartiallyCancelled = "PartiallyCancelled";

    /// <summary>
    /// Bookings of which all rooms are cancelled.
    /// </summary>
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Bookings with a refund that still has to be paid back, fully or partly cancelled.
    /// </summary>
    public const string RefundPending = "RefundPending";

    /// <summary>
    /// Bookings of which all decided refunds are paid back.
    /// </summary>
    public const string Refunded = "Refunded";

    /// <summary>
    /// Values about the money of the booking, the "Payment status" group of the filter.
    /// </summary>
    public static readonly IReadOnlyList<string> PaymentValues = [Pending, Paid, RefundPending, Refunded];

    /// <summary>
    /// Values about the cancelled rooms, the "Cancellation status" group of the filter.
    /// </summary>
    public static readonly IReadOnlyList<string> CancellationValues = [NotCancelled, PartiallyCancelled, Cancelled];

    /// <summary>
    /// All values, in the order of the filter on the booking list.
    /// </summary>
    public static readonly IReadOnlyList<string> Values = [All, .. PaymentValues, .. CancellationValues];
}
