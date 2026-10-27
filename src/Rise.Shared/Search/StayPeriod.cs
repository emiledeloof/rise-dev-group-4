using System.Text.Json.Serialization;

namespace Rise.Shared.Search;

/// <summary>
/// requests used by the room search
/// </summary>
public static partial class SearchRequest
{
    /// <summary>
    /// dates and guest requirements of the stay the guest is looking for,
    /// used by the search form and the availability endpoint
    /// </summary>
    public class StayPeriod
    {
        /// <summary>
        /// minimum number of adults required for a booking
        /// </summary>
        public const int MinAdults = 1;

        /// <summary>
        /// default maximum guest capacity of the estate when dynamic data is not yet loaded
        /// </summary>
        public const int DefaultMaxCapacity = 6;

        /// <summary>
        /// fallback constant for initial resort capacity (backward-compatible)
        /// </summary>
        public const int MaxCapacity = DefaultMaxCapacity;

        /// <summary>
        /// default number of adults in search forms
        /// </summary>
        public const int DefaultAdults = 2;

        /// <summary>
        /// arrival date, null until the guest picks one
        /// </summary>
        public DateOnly? CheckIn { get; set; }

        /// <summary>
        /// departure date, null until the guest picks one
        /// </summary>
        public DateOnly? CheckOut { get; set; }

        /// <summary>
        /// number of adult guests, defaults to 2
        /// </summary>
        public int Adults { get; set; } = DefaultAdults;

        /// <summary>
        /// dynamic maximum guest capacity of the estate, defaults to DefaultMaxCapacity and can be updated from room data
        /// </summary>
        public int? DynamicMaxCapacity { get; set; }

        /// <summary>
        /// nb of nights between check in and check out, 0 when a date is missing or the period is invalid
        /// </summary>
        [JsonIgnore]
        public int Nights => CheckIn is { } checkIn && CheckOut is { } checkOut && checkOut > checkIn
            ? checkOut.DayNumber - checkIn.DayNumber
            : 0;

        /// <summary>
        /// rules for a valid stay, same rules on the client form and on the server
        /// </summary>
        public class Validator : AbstractValidator<StayPeriod>
        {
            /// <summary>
            /// sets up the date and guest validation rules
            /// </summary>
            public Validator()
            {
                // TODO: translate the messages with the languages task

                RuleFor(x => x.CheckIn)
                    .NotEmpty()
                    .WithMessage("Please choose a check-in date.");

                // today is based on the machine clock, browser on the client and utc on the server
                // TODO: use the sardinia timezone once we handle bookings around midnight
                RuleFor(x => x.CheckIn)
                    .Must(checkIn => checkIn >= DateOnly.FromDateTime(DateTime.Today))
                    .When(x => x.CheckIn.HasValue)
                    .WithMessage("Check-in can't be in the past.");

                RuleFor(x => x.CheckOut)
                    .NotEmpty()
                    .WithMessage("Please choose a check-out date.");

                // same rule as Booking.ChangeDates in the domain, shared cannot reference domain so it is repeated here
                RuleFor(x => x.CheckOut)
                    .Must((period, checkOut) => checkOut > period.CheckIn)
                    .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
                    .WithMessage("Check-out must be after check-in.");

                // guest count rules (ENOG acceptance criteria: At least 1 adult required, maximum capacity can't be exceeded)
                RuleFor(x => x.Adults)
                    .GreaterThanOrEqualTo(MinAdults)
                    .WithMessage("At least 1 adult is required.");

                RuleFor(x => x.Adults)
                    .Must((period, adults) => adults <= (period.DynamicMaxCapacity ?? MaxCapacity))
                    .WithMessage(period => $"Maximum capacity of {period.DynamicMaxCapacity ?? MaxCapacity} guests cannot be exceeded.");
            }
        }
    }
}
