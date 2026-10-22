using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.Rooms;
using Rise.Shared.Rates;

namespace Rise.Client.Rates;

public partial class Rates : ComponentBase
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

    //create new rate
    private bool _isCreateOpen;
    private bool _isCreating;
    private string? _createErrorMessage;
    private IEnumerable<string> _createRoomNames = [];
    private RateRequest.Create _createModel = NewRateModel();


    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    [Inject] public required IRateService RateService { get; set; }

    [Inject] public required IRoomService RoomService { get; set; }

    [Inject] public required ILogger<Rates> Logger { get; set; }

    protected override async Task OnInitializedAsync()
        => await LoadPageAsync();

    private static RateRequest.Create NewRateModel() => new()
    {
        Name = string.Empty,
        RoomName = string.Empty,
        Description = string.Empty,
        PricePerNight = 0,
    };

    private string CreateRateHeading
    {
        get
        {
            var name = _createModel.Name.Trim();
            var policy = _createModel.Description?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return "New rate";

            return string.IsNullOrWhiteSpace(policy)
                ? name
                : $"{name} · {policy}";
        }
    }

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

    private async Task OpenCreateFormAsync()
    {
        _createModel = NewRateModel();
        _createErrorMessage = null;
        _isCreateOpen = true;

        try
        {
            var result = await RoomService.GetListAsync(new RoomRequest.List { Take = 20 });
            if (!result.IsSuccess)
            {
                _createErrorMessage = GetErrorMessage(result, "Rooms could not be loaded.");
                return;
            }

            _createRoomNames = result.Value.Rooms
                .Select(room => room.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not load rooms for rate creation.");
            _createErrorMessage = "Could not contact the server to load rooms. Please try again.";
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "The room list response could not be read for rate creation.");
            _createErrorMessage = "The server returned an unreadable room list. Please try again.";
        }
    }

    private void CloseCreateForm()
    {
        _isCreateOpen = false;
        _createErrorMessage = null;
    }

    private async Task CreateRateAsync()
    {
        _isCreating = true;
        _createErrorMessage = null;

        try
        {
            var result = await RateService.CreateAsync(_createModel);
            if (!result.IsSuccess)
            {
                _createErrorMessage = GetErrorMessage(result, "The rate could not be created.");
                return;
            }

            _selectedRoomName = null;
            _selectedPolicy = null;
            _currentPage = 1;
            CloseCreateForm();
            await LoadPageAsync();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not send the create-rate request.");
            _createErrorMessage = "Could not contact the server to create the rate. Please try again.";
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "The create-rate response could not be read.");
            _createErrorMessage = "The server returned an unreadable response. Please try again.";
        }
        finally
        {
            _isCreating = false;
        }
    }

    private static string GetErrorMessage(Ardalis.Result.IResult result, string fallback)
    {
        var messages = result.ValidationErrors.Select(error => error.ErrorMessage)
            .Concat(result.Errors)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();

        return messages.Count == 0 ? fallback : string.Join(" ", messages);
    }

    private static string FormatPrice(decimal price)
        => $"€{price.ToString("0.00", CultureInfo.InvariantCulture)}";


    private static string FormatDate(DateOnly date)
        => date.ToString("dd MMM", CultureInfo.InvariantCulture);
}