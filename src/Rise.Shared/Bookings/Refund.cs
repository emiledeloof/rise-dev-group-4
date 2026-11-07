namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// Records that the refund of a cancelled room was paid back by hand, sent from the admin booking detail page.
    /// The website never pays the refund itself, it only keeps track so no refund gets forgotten.
    /// </summary>
    public sealed class RecordRefund
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/rooms/{BookingRoomId}/refund
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// From the route, the cancelled room whose refund was paid.
        /// </summary>
        public int BookingRoomId { get; set; }

        /// <summary>
        /// Day the refund was paid, defaults to today in the form.
        /// </summary>
        public DateOnly PaidOn { get; set; }

        /// <summary>
        /// What was actually paid back, defaults to the decided refund but can differ, checked again by the server.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional, eg the Stripe refund id or "bank transfer".
        /// </summary>
        public string? Note { get; set; }

        public class Validator : AbstractValidator<RecordRefund>
        {
            /// <summary>
            /// Same as Booking.RefundNoteMaxLength, Shared can't reference Domain.
            /// </summary>
            public const int NoteMaxLength = 500;

            public Validator()
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0)
                    .WithMessage("Please enter the amount that was paid back.");
                // * 1 day of slack, the browser of the admin can already be on the next day while the server is still in UTC
                RuleFor(x => x.PaidOn)
                    .Must(paidOn => paidOn <= DateOnly.FromDateTime(DateTime.Today).AddDays(1))
                    .WithMessage("The payment date can't be in the future.");
                RuleFor(x => x.Note)
                    .MaximumLength(NoteMaxLength);
            }
        }
    }

    /// <summary>
    /// Undoes a refund payment recorded by mistake, the refund is open again.
    /// </summary>
    public sealed class UndoRefund
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/rooms/{BookingRoomId}/refund
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// From the route.
        /// </summary>
        public int BookingRoomId { get; set; }
    }
}

public static partial class BookingResponse
{
    /// <summary>
    /// Answer to <see cref="BookingRequest.RecordRefund"/> and <see cref="BookingRequest.UndoRefund"/>.
    /// </summary>
    public sealed class Refund
    {
        public required int BookingId { get; init; }
        public required int BookingRoomId { get; init; }

        /// <summary>
        /// One of <see cref="BookingRefundStatus"/>, the refund status of the whole booking after the change.
        /// </summary>
        public required string RefundStatus { get; init; }

        /// <summary>
        /// Sum of the refunds of the booking that still have to be paid back.
        /// </summary>
        public required decimal RefundOpenAmount { get; init; }
    }
}
