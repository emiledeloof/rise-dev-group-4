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
    // values of the first select, it only picks which detailed statuses the second select offers
    private const string AllCategory = "All";
    private const string PaymentCategory = "Payment";
    private const string CancellationCategory = "Cancellation";

    private string _category = AllCategory;
    private string _statusFilter = BookingStatusFilters.All;
    private int _loadVersion;

    /// <summary>
    /// detailed statuses of the chosen category, empty for all bookings so the second select is hidden
    /// </summary>
    private IReadOnlyList<string> CategoryValues => _category switch
    {
        PaymentCategory => BookingStatusFilters.PaymentValues,
        CancellationCategory => BookingStatusFilters.CancellationValues,
        _ => [],
    };

    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    private string ListTitle => _statusFilter switch
    {
        BookingStatusFilters.All => "All bookings",
        _ => $"{FilterLabel(_statusFilter)} bookings",
    };

    private string EmptyMessage => _statusFilter switch
    {
        BookingStatusFilters.All => "No bookings yet.",
        BookingStatusFilters.NotCancelled => "No bookings that are not cancelled.",
        _ => $"No {FilterLabel(_statusFilter).ToLowerInvariant()} bookings.",
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
            Status = _statusFilter,
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

    /// <summary>
    /// label of a filter value in the select, the list title and the empty msg
    /// </summary>
    private static string FilterLabel(string filter) => filter switch
    {
        BookingStatusFilters.NotCancelled => "Not cancelled",
        BookingStatusFilters.Pending => "Pending",
        BookingStatusFilters.Paid => "Paid",
        BookingStatusFilters.PartiallyCancelled => "Partially cancelled",
        BookingStatusFilters.Cancelled => "Cancelled",
        BookingStatusFilters.RefundPending => "Refund pending",
        BookingStatusFilters.Refunded => "Refunded",
        _ => "All bookings",
    };

    /// <summary>
    /// a new category starts on its first detailed status, so the list always matches what both selects show
    /// </summary>
    private async Task ApplyCategoryAsync()
    {
        _statusFilter = CategoryValues.Count > 0 ? CategoryValues[0] : BookingStatusFilters.All;
        await ApplyFilterAsync();
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
