using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.Rooms;
using Rise.Shared.Rates;
namespace Rise.Client.Rates;

public partial class Rates1 : ComponentBase
{
    private const int PageSize = 10;

    private IEnumerable<RateDto.Index>? _rates;
    private string? _errorMessage;
    private int _currentPage = 1;
    private int _totalCount;

    //drop down filters
    private string? _selectedRoomName;
    private string? _selectedPolicy;
    private IEnumerable<string> _roomNames = [];
    private IEnumerable<string> _policies = [];

    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    [Inject] public required IRateService RateService { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadPageAsync();

    private async Task LoadPageAsync()
    {
        _rates = null; // show loading state
        _errorMessage = null;

        var request = new RateRequest.Index
        {
            Skip = (_currentPage - 1) * PageSize,
            Take = PageSize,
            OrderDescending = true,
            RoomName = _selectedRoomName,
            Policy = _selectedPolicy,
        };

        var result = await RateService.GetIndexAsync(request);

        if (!result.IsSuccess)
        {
            _errorMessage = "The rates could not be loaded.";
            _rates = [];
            return;
        }

        _rates = result.Value.Rates;
        _totalCount = result.Value.TotalCount;
        _roomNames = result.Value.RoomNames;
        _policies = result.Value.Policies;
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

    private async Task ApplyFiltersAsync()
    {
        _currentPage = 1;
        await LoadPageAsync();
    }

    private static string FormatPrice(decimal price)
        => $"€{price.ToString("0.##", CultureInfo.InvariantCulture)}";

    private static string FormatDate(DateOnly date)
        => date.ToString("dd MMM", CultureInfo.InvariantCulture);
}