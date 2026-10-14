using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;
using Rise.Shared.Common;

namespace Rise.Client.Pages;

public partial class BookingList
{
    private const int PageSize = 10;

    private IEnumerable<BookingDto.Index>? _bookings;
    private string? _errorMessage;
    private int _currentPage = 1;
    private int _totalCount;

    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    [Inject] public required IBookingService BookingService { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadPageAsync();

    private async Task LoadPageAsync()
    {
        _bookings = null; // show loading state
        _errorMessage = null;

        var request = new QueryRequest.SkipTake
        {
            Skip = (_currentPage - 1) * PageSize,
            Take = PageSize,
            OrderDescending = true, // newest first
        };

        var result = await BookingService.GetIndexAsync(request);

        if (!result.IsSuccess)
        {
            _errorMessage = "The bookings could not be loaded.";
            _bookings = [];
            return;
        }

        _bookings = result.Value.Bookings;
        _totalCount = result.Value.TotalCount;
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

    // same format as the invoice list, eg €432.00
    private static string FormatAmount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string FormatDate(DateOnly date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
