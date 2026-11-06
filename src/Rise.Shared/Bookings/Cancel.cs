namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// Cancels one or more rooms of a booking, sent by the admin from the booking detail page.
    /// Sending all active rooms cancels the whole booking.
    /// The refunds are only recorded, the admin pays them back by hand, the website never refunds via Stripe.
    /// </summary>
    public sealed class Cancel
    {
        /// <summary>
        /// From the route, /api/bookings/{BookingId}/cancel
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Why the rooms are cancelled, kept on each cancelled room for later.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// The rooms to cancel, each with its own refund because each room has its own cancellation policy.
        /// </summary>
        public List<RoomSelection> Rooms { get; set; } = [];

        /// <summary>
        /// New nb of guests when the current nb doesn't fit in the remaining rooms, null keeps the current nb.
        /// </summary>
        public int? Adults { get; set; }

        /// <summary>
        /// A room to cancel and its refund.
        /// </summary>
        public sealed class RoomSelection
        {
            public int BookingRoomId { get; set; }

            /// <summary>
            /// Refund the admin decided on, at most the price of the room, checked again by the server.
            /// </summary>
            public decimal RefundAmount { get; set; }
        }

        public class Validator : AbstractValidator<Cancel>
        {
            public Validator()
            {
                RuleFor(x => x.Reason)
                    .NotEmpty()
                    .WithMessage("Please enter a reason for the cancellation.")
                    .MaximumLength(500);
                RuleFor(x => x.Rooms)
                    .NotEmpty()
                    .WithMessage("Please select at least one room to cancel.")
                    .Must(rooms => rooms.Select(x => x.BookingRoomId).Distinct().Count() == rooms.Count)
                    .WithMessage("A room can only be cancelled once.");
                RuleForEach(x => x.Rooms).ChildRules(room =>
                {
                    room.RuleFor(x => x.RefundAmount)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage("The refund can't be negative.");
                });
                RuleFor(x => x.Adults)
                    .GreaterThanOrEqualTo(1)
                    .When(x => x.Adults is not null)
                    .WithMessage("Please enter at least 1 guest.");
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

        /// <summary>
        /// Sum of the refunds of this cancellation.
        /// </summary>
        public required decimal RefundAmount { get; init; }

        /// <summary>
        /// Nb of rooms cancelled by this cancellation.
        /// </summary>
        public required int CancelledRoomCount { get; init; }

        /// <summary>
        /// Nb of rooms still booked, 0 means the whole booking is cancelled.
        /// </summary>
        public required int RemainingRoomCount { get; init; }
    }
}
