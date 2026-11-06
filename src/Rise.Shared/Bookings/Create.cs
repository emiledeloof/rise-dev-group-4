using Rise.Shared.Guests;
using Rise.Shared.Search;

namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// Everything needed to save a booking, sent once when the guest clicks "Pay now".
    /// The server re-checks availability and recalculates the price, the client prices are only for display.
    /// </summary>
    public class Create
    {
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }

        /// <summary>
        /// Nb of guests of the stay, the server checks it against the capacity of the selected rooms.
        /// </summary>
        public int Adults { get; set; } = SearchRequest.StayPeriod.DefaultAdults;

        /// <summary>
        /// Guest details, an existing guest with the same email is reused and updated.
        /// </summary>
        public GuestRequest.Create Guest { get; set; } = new();

        /// <summary>
        /// The selected rooms, each with the rate the guest chose.
        /// </summary>
        public List<RoomSelection> Rooms { get; set; } = [];

        /// <summary>
        /// A room of the booking with its chosen rate.
        /// </summary>
        public class RoomSelection
        {
            public int RoomId { get; set; }
            public int RateId { get; set; }
        }

        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                // same date rules as SearchRequest.StayPeriod.Validator
                RuleFor(x => x.CheckIn)
                    .NotEmpty()
                    .Must(checkIn => checkIn >= DateOnly.FromDateTime(DateTime.Today))
                    .WithMessage("Check-in can't be in the past.");
                RuleFor(x => x.CheckOut)
                    .GreaterThan(x => x.CheckIn)
                    .WithMessage("Check-out must be after check-in.");

                RuleFor(x => x.Adults)
                    .GreaterThanOrEqualTo(SearchRequest.StayPeriod.MinAdults)
                    .WithMessage($"At least {SearchRequest.StayPeriod.MinAdults} guest is required.");

                RuleFor(x => x.Rooms)
                    .NotEmpty()
                    .WithMessage("Please select at least one room.")
                    .Must(rooms => rooms.Select(r => r.RoomId).Distinct().Count() == rooms.Count)
                    .WithMessage("A room can only be booked once.");
                RuleForEach(x => x.Rooms).ChildRules(room =>
                {
                    room.RuleFor(r => r.RoomId).GreaterThan(0);
                    room.RuleFor(r => r.RateId).GreaterThan(0);
                });

                RuleFor(x => x.Guest)
                    .NotNull()
                    .SetValidator(new GuestRequest.Create.Validator());
            }
        }
    }
}

public static partial class BookingResponse
{
    public class Create
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;

        /// <summary>
        /// Total of the stay as calculated by the server.
        /// </summary>
        public decimal TotalAmount { get; set; }
    }

    public class Index
    {
        public IEnumerable<BookingDto.Index> Bookings { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
