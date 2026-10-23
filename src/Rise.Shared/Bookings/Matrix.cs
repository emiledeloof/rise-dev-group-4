namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    /// <summary>
    /// Parameters for the admin booking matrix: the start of the visible date window and its width.
    /// </summary>
    public sealed class Matrix
    {
        /// <summary>
        /// First day shown in the calendar grid (inclusive). Defaults to yesterday so today is
        /// always visible.
        /// </summary>
        public DateOnly WindowStart { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        /// <summary>
        /// Number of days to display. Default is 16 (matching the reference design).
        /// </summary>
        public int WindowDays { get; set; } = 16;
    }
}

public static partial class BookingResponse
{
    /// <summary>
    /// The full booking matrix: all rooms as rows, each with the bookings that fall inside the window.
    /// </summary>
    public sealed class Matrix
    {
        public required DateOnly WindowStart { get; init; }
        public required int WindowDays { get; init; }
        public required List<BookingDto.MatrixRow> Rows { get; init; }
    }
}