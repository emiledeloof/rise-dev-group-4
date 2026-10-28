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
        CellStatus Status,
        BookingDto.MatrixBooking? Booking
    );

    private List<MatrixCellItem> GetRowCells(BookingDto.MatrixRow row)
    {
        var items = new List<MatrixCellItem>();
        var dayIndex = 0;

        while (dayIndex < WindowDays)
        {
            var day = _windowStart.AddDays(dayIndex);

            // Prefer Occupied over Temporary when both somehow cover the same day
            var booking =
                row.Bookings.FirstOrDefault(b => b.Status == CellStatus.Occupied && b.CheckIn <= day && b.CheckOut > day)
                ?? row.Bookings.FirstOrDefault(b => b.Status == CellStatus.Temporary && b.CheckIn <= day && b.CheckOut > day);

            if (booking is null)
            {
                // Accumulate consecutive available days (stopping at the next booking)
                var freeStart = dayIndex;
                while (dayIndex < WindowDays)
                {
                    var nextDay = _windowStart.AddDays(dayIndex);
                    var hitsBooking = row.Bookings.Any(b =>
                        (b.Status == CellStatus.Occupied || b.Status == CellStatus.Temporary)
                        && b.CheckIn <= nextDay && b.CheckOut > nextDay);
                    if (hitsBooking)
                        break;
                    dayIndex++;
                }
                items.Add(new MatrixCellItem(dayIndex - freeStart, CellStatus.Available, null));
            }
            else
            {
                // Span until the booking's checkout (clamped to the window)
                var bookingStart = dayIndex;
                while (dayIndex < WindowDays)
                {
                    var nextDay = _windowStart.AddDays(dayIndex);
                    if (nextDay >= booking.CheckOut)
                        break;
                    dayIndex++;
                }
                items.Add(new MatrixCellItem(dayIndex - bookingStart, booking.Status, booking));
            }
        }

        return items;
    }
}

