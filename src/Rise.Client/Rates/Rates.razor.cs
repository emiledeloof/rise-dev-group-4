using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.RatePricing;
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

    // edit a rate, values of the open form, null when no rate is being edited
    private RatePricingRequest.Edit? _editModel;

    // the rate as it was when edit was clicked, compared on save to know what changed
    private RateDto.Index? _originalRate;

    // shown under the table after a save that worked
    private string? _successMessage;

    // shown in the form when the api refused or failed the save
    private string? _saveErrorMessage;


    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    [Inject] public required IRateService RateService { get; set; }

    [Inject] public required IRoomService RoomService { get; set; }

    /// <summary>
    /// saves the edit form, same api as before the merge of the rate pricing page
    /// </summary>
    [Inject] public required IRatePricingService RatePricingService { get; set; }

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
        // * one form at a time, opening create closes edit
        _editModel = null;
        _successMessage = null;
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

    /// <summary>
    /// opens the edit form for one rate, prefilled with its current values
    /// </summary>
    private void StartEdit(RateDto.Index rate)
    {
        // msgs of a previous save dont belong to this edit, one form at a time
        _saveErrorMessage = null;
        _successMessage = null;
        _isCreateOpen = false;

        // kept to compare with the form on save
        _originalRate = rate;

        // * a copy, typing in the form doesnt change the table before the save
        _editModel = new RatePricingRequest.Edit
        {
            RateId = rate.Id,
            PricePerNight = rate.PricePerNight,
            MinNights = rate.MinNights,
            IsRefundable = rate.IsRefundable,
            FullRefundDays = rate.FullRefundDays,
            PartialRefundDays = rate.PartialRefundDays,
            PartialRefundPercent = rate.PartialRefundPercent
        };
    }

    /// <summary>
    /// closes the edit form without saving
    /// </summary>
    private void CancelEdit()
    {
        _editModel = null;
    }

    /// <summary>
    /// sends the edit form to the api, only called when the validator accepts it
    /// </summary>
    private async Task SaveEditAsync()
    {
        var result = await RatePricingService.EditAsync(_editModel!);

        // form stays open so the admin can retry without typing again
        if (!result.IsSuccess)
        {
            _saveErrorMessage = "The changes could not be saved. Please try again.";
            return;
        }

        // ! compared before closing the form, _editModel is null after
        // local copies so the null checks are done once
        var edit = _editModel!;
        var original = _originalRate!;

        var priceChanged = edit.PricePerNight != original.PricePerNight;
        var minNightsChanged = edit.MinNights != original.MinNights;
        // refundable or not, the checkbox
        var refundableChanged = edit.IsRefundable != original.IsRefundable;

        // ! refund values only count for a refundable rate, the service ignores them otherwise
        var fullRefundChanged = edit.IsRefundable
            && edit.FullRefundDays != original.FullRefundDays;
        var partialRefundChanged = edit.IsRefundable
            && (edit.PartialRefundDays != original.PartialRefundDays
                || edit.PartialRefundPercent != original.PartialRefundPercent);

        // * one msg for every combination, eg saved price and partial refund
        var changes = new List<string>();
        if (priceChanged) changes.Add("price");
        if (minNightsChanged) changes.Add("minimum nights");
        if (refundableChanged) changes.Add(edit.IsRefundable ? "cancellation policy (refundable)" : "cancellation policy (non-refundable)");
        if (fullRefundChanged) changes.Add("full refund period");
        if (partialRefundChanged) changes.Add("partial refund");

        // close the form and reload so the table shows the saved values, the page and filters stay
        _editModel = null;
        await LoadPageAsync();

        _successMessage = changes.Count == 0
            ? "Nothing was changed."
            : $"Saved: {JoinWithAnd(changes)}.";
    }

    /// <summary>
    /// text of the policy for the table, eg 100 pct up to 30 days, 50 pct up to 7 days
    /// </summary>
    private static string FormatPolicy(RateDto.Index rate)
        => rate.IsRefundable
            ? $"100% up to {rate.FullRefundDays} days, {rate.PartialRefundPercent}% up to {rate.PartialRefundDays} days"
            : RatePolicies.NonRefundable;

    /// <summary>
    /// joins the words with commas and a last and, eg price, minimum nights and cancellation policy
    /// </summary>
    private static string JoinWithAnd(List<string> words)
        => words.Count == 1
            ? words[0]
            : $"{string.Join(", ", words.SkipLast(1))} and {words[^1]}";

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