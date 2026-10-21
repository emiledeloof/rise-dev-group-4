using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Pages;

public partial class BookingMatrix
{
    private const int WindowDays = 16;

    // Start yesterday so today is always visible in the grid
    private DateOnly _windowStart = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

    private BookingResponse.Matrix? _matrix;
    private string? _errorMessage;

    [Inject] public required IBookingService BookingService { get; set; }

    private IEnumerable<DateOnly> Days =>
        Enumerable.Range(0, WindowDays).Select(i => _windowStart.AddDays(i));

    private string WindowLabel =>
        $"{_windowStart:d MMM} – {_windowStart.AddDays(WindowDays - 1):d MMM yyyy}";

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _matrix = null;
        _errorMessage = null;

        var result = await BookingService.GetMatrixAsync(new BookingRequest.Matrix
        {
            WindowStart = _windowStart,
            WindowDays = WindowDays,
        });

        if (!result.IsSuccess)
        {
            _errorMessage = "Could not load the booking matrix.";
            return;
        }

        _matrix = result.Value;
    }

    private async Task PreviousWindowAsync()
    {
        _windowStart = _windowStart.AddDays(-WindowDays);
        await LoadAsync();
    }

    private async Task NextWindowAsync()
    {
        _windowStart = _windowStart.AddDays(WindowDays);
        await LoadAsync();
    }

    private static bool IsToday(DateOnly day)
        => day == DateOnly.FromDateTime(DateTime.Today);

    public record MatrixCellItem(
        int ColSpan,
        bool IsBooking,
        BookingDto.MatrixBooking? Booking
    );

    private List<MatrixCellItem> GetRowCells(BookingDto.MatrixRow row)
    {
        var items = new List<MatrixCellItem>();
        var dayIndex = 0;

        while (dayIndex < WindowDays)
        {
            var day = _windowStart.AddDays(dayIndex);
            var booking = row.Bookings.FirstOrDefault(b => b.CheckIn <= day && b.CheckOut > day);

            if (booking is null)
            {
                // Count consecutive free days
                var freeStart = dayIndex;
                while (dayIndex < WindowDays)
                {
                    var nextDay = _windowStart.AddDays(dayIndex);
                    if (row.Bookings.Any(b => b.CheckIn <= nextDay && b.CheckOut > nextDay))
                    {
                        break;
                    }
                    dayIndex++;
                }
                var span = dayIndex - freeStart;
                items.Add(new MatrixCellItem(span, false, null));
            }
            else
            {
                // Count how many days this booking spans within the current window starting at dayIndex
                var bookingStart = dayIndex;
                while (dayIndex < WindowDays)
                {
                    var nextDay = _windowStart.AddDays(dayIndex);
                    if (nextDay >= booking.CheckOut)
                    {
                        break;
                    }
                    dayIndex++;
                }
                var span = dayIndex - bookingStart;
                items.Add(new MatrixCellItem(span, true, booking));
            }
        }

        return items;
    }
}

