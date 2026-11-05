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
        /// One of <see cref="BookingCancellationFilters"/>, empty shows all bookings.
        /// </summary>
        public string? CancellationFilter { get; set; }

        public override string ToString()
            => $"{base.ToString()}&CancellationFilter={Uri.EscapeDataString(CancellationFilter ?? "")}";

        public class Validator : AbstractValidator<Index>
        {
            public Validator()
            {
                RuleFor(x => x.CancellationFilter)
                    .Must(x => string.IsNullOrEmpty(x)
                        || x is BookingCancellationFilters.All
                            or BookingCancellationFilters.NotCancelled
                            or BookingCancellationFilters.OnlyCancelled)
                    .WithMessage("Please choose a valid cancellation filter.");
            }
        }
    }
}

/// <summary>
/// values of the cancellation filter on the admin booking list
/// </summary>
public static class BookingCancellationFilters
{
    public const string All = "All";
    public const string NotCancelled = "NotCancelled";
    public const string OnlyCancelled = "OnlyCancelled";
}
