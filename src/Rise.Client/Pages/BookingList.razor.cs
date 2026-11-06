using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Pages;

public partial class BookingList
{
    private const int PageSize = 10;

    private IEnumerable<BookingDto.Index>? _bookings;
    private string? _errorMessage;
    private int _currentPage = 1;
    private int _totalCount;
    private string _cancellationFilter = BookingCancellationFilters.All;
    private int _loadVersion;

    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    private string ListTitle => _cancellationFilter switch
    {
        BookingCancellationFilters.NotCancelled => "Not cancelled bookings",
        BookingCancellationFilters.OnlyCancelled => "Cancelled bookings",
        _ => "All bookings",
    };

    private string EmptyMessage => _cancellationFilter switch
    {
        BookingCancellationFilters.NotCancelled => "No bookings that are not cancelled.",
        BookingCancellationFilters.OnlyCancelled => "No cancelled bookings.",
        _ => "No bookings yet.",
    };

    [Inject] public required IBookingService BookingService { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadPageAsync();

    private async Task LoadPageAsync()
    {
        _bookings = null; // show loading state
        _errorMessage = null;
        var version = ++_loadVersion;

        var request = new BookingRequest.Index
        {
            Skip = (_currentPage - 1) * PageSize,
            Take = PageSize,
            OrderDescending = true, // newest first
            CancellationFilter = _cancellationFilter,
        };

        var result = await BookingService.GetIndexAsync(request);

        // the filter stays usable while loading, an older answer must not replace a newer one
        if (version != _loadVersion)
            return;

        if (!result.IsSuccess)
        {
            _errorMessage = "The bookings could not be loaded.";
            _bookings = [];
            return;
        }

        _bookings = result.Value.Bookings;
        _totalCount = result.Value.TotalCount;
    }

    private async Task ApplyFilterAsync()
    {
        _currentPage = 1;
        await LoadPageAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            await LoadPageAsync();
        }
    }

    private async Task NextPageAsync()
    {
        if (_currentPage < TotalPages)
        {
            _currentPage++;
            await LoadPageAsync();
        }
    }

    private async Task GoToPageAsync(int page)
    {
        if (page != _currentPage && page >= 1 && page <= TotalPages)
        {
            _currentPage = page;
            await LoadPageAsync();
        }
    }
}
