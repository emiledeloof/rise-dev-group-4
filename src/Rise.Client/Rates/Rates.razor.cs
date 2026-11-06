using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Common;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;
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
    private string? _selectedDate;
    private IEnumerable<string> _roomNames = [];
    private IEnumerable<string> _policies = [];
    private IEnumerable<string> _dateRanges = [];

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
    private string? _activationErrorMessage;
    private readonly HashSet<int> _pendingRateActivations = [];
    private bool _isSettingsOpen;
    private bool _isSavingSettings;
    private bool _settingsLoaded;
    private string? _settingsLoadErrorMessage;
    private string? _settingsErrorMessage;
    private GeneralSettingsDto? _generalSettings;
    private GeneralSettingsRequest.Update _generalSettingsModel = new();

    private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);

    private bool IsVatApplied => _generalSettings is { IsVatEnabled: true, VatPercentage: > 0m };

    [Inject] public required IRateService RateService { get; set; }

    [Inject] public required IRoomService RoomService { get; set; }

    /// <summary>
    /// saves the edit form, same api as before the merge of the rate pricing page
    /// </summary>
    [Inject] public required IRatePricingService RatePricingService { get; set; }

    [Inject] public required ILogger<Rates> Logger { get; set; }

    protected override async Task OnInitializedAsync()
        => await Task.WhenAll(LoadPageAsync(), LoadGeneralSettingsAsync());

    private async Task LoadGeneralSettingsAsync()
    {
        _settingsLoadErrorMessage = null;
        try
        {
            var result = await RatePricingService.GetGeneralSettingsAsync();
            if (!result.IsSuccess)
            {
                _settingsLoadErrorMessage = GetErrorMessage(result, "General settings could not be loaded.");
                return;
            }

            _generalSettings = result.Value;
            _generalSettingsModel = new GeneralSettingsRequest.Update
            {
                IsVatEnabled = result.Value.IsVatEnabled,
                VatPercentage = result.Value.VatPercentage,
            };
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not load general settings.");
            _settingsLoadErrorMessage = "Could not contact the server to load general settings. Please try again.";
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "The general-settings response could not be read.");
            _settingsLoadErrorMessage = "The server returned unreadable general settings. Please try again.";
        }
        finally
        {
            _settingsLoaded = true;
        }
    }

    private void ToggleSettingsForm()
    {
        if (_isSettingsOpen)
        {
            CloseSettingsForm();
            return;
        }

        _isCreateOpen = false;
        _editModel = null;
        _successMessage = null;
        _settingsErrorMessage = null;
        _generalSettingsModel = new GeneralSettingsRequest.Update
        {
            IsVatEnabled = _generalSettings!.IsVatEnabled,
            VatPercentage = _generalSettings.VatPercentage,
        };
        _isSettingsOpen = true;
    }

    private void CloseSettingsForm()
    {
        _isSettingsOpen = false;
        _settingsErrorMessage = null;
    }

    private async Task SaveGeneralSettingsAsync()
    {
        _isSavingSettings = true;
        _settingsErrorMessage = null;
        try
        {
            var result = await RatePricingService.UpdateGeneralSettingsAsync(_generalSettingsModel);
            if (!result.IsSuccess)
            {
                _settingsErrorMessage = GetErrorMessage(result, "General settings could not be saved.");
                return;
            }

            _generalSettings = result.Value;
            _isSettingsOpen = false;
            _successMessage = "General settings saved.";
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not save general settings.");
            _settingsErrorMessage = "Could not contact the server to save general settings. Please try again.";
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "The general-settings response could not be read after saving.");
            _settingsErrorMessage = "The server returned an unreadable response. Please try again.";
        }
        finally
        {
            _isSavingSettings = false;
        }
    }

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

        var selectedRange = ParseDateRange(_selectedDate);
        var request = new RateRequest.Index
        {
            Skip = (_currentPage - 1) * PageSize,
            Take = PageSize,
            OrderDescending = true,
            RoomName = _selectedRoomName,
            Policy = _selectedPolicy,
            StartDate = selectedRange.StartDate,
            EndDate = selectedRange.EndDate,
            FilterByDateRange = !string.IsNullOrEmpty(_selectedDate)
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
        _dateRanges = result.Value.DateRanges;
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
        _isSettingsOpen = false;
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
        _isSettingsOpen = false;
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
        _isSettingsOpen = false;

        // kept to compare with the form on save
        _originalRate = rate;

        // * a copy, typing in the form doesnt change the table before the save
        _editModel = new RatePricingRequest.Edit
        {
            RateId = rate.Id,
            PricePerNight = rate.PricePerNight,
            IsActive = rate.IsActive,
            MinNights = rate.MinNights,
            IsRefundable = rate.IsRefundable,
            FullRefundDays = rate.FullRefundDays,
            PartialRefundDays = rate.PartialRefundDays,
            PartialRefundPercent = rate.PartialRefundPercent,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate
        };
    }

    private void ClearEditDates()
    {
        if (_editModel is null)
            return;

        _editModel.StartDate = null;
        _editModel.EndDate = null;
    }

    private void ClearCreateDates()
    {
        _createModel.StartDate = null;
        _createModel.EndDate = null;
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
        _editModel!.IsActive = _originalRate!.IsActive;
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
        var activeChanged = edit.IsActive != original.IsActive;
        var minNightsChanged = edit.MinNights != original.MinNights;
        var datesChanged = edit.StartDate != original.StartDate || edit.EndDate != original.EndDate;
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
        if (activeChanged) changes.Add(edit.IsActive ? "activated rate" : "deactivated rate");
        if (minNightsChanged) changes.Add("minimum nights");
        if (datesChanged) changes.Add("availability dates");
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

    private async Task SetRateActiveAsync(RateDto.Index rate, ChangeEventArgs args)
    {
        if (args.Value is not bool isActive || isActive == rate.IsActive
            || !_pendingRateActivations.Add(rate.Id))
        {
            return;
        }

        _activationErrorMessage = null;

        try
        {
            var result = await RatePricingService.EditAsync(new RatePricingRequest.Edit
            {
                RateId = rate.Id,
                PricePerNight = rate.PricePerNight,
                IsActive = isActive,
                MinNights = rate.MinNights,
                IsRefundable = rate.IsRefundable,
                FullRefundDays = rate.FullRefundDays,
                PartialRefundDays = rate.PartialRefundDays,
                PartialRefundPercent = rate.PartialRefundPercent,
            });

            if (!result.IsSuccess)
            {
                _activationErrorMessage = "The rate status could not be updated. Please try again.";
                return;
            }

            rate.IsActive = isActive;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not update active status for rate {RateId}.", rate.Id);
            _activationErrorMessage = "Could not contact the server to update the rate status. Please try again.";
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "The active-status response could not be read for rate {RateId}.", rate.Id);
            _activationErrorMessage = "The server returned an unreadable response. Please try again.";
        }
        finally
        {
            _pendingRateActivations.Remove(rate.Id);
        }
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

    private string FormatPriceIncludingVat(decimal price)
        => FormatPrice(price * (1m + _generalSettings!.VatPercentage / 100m));

    private static string FormatPercentage(decimal percentage)
        => $"{percentage.ToString("0.##", CultureInfo.InvariantCulture)}%";


    private static string FormatDate(DateOnly date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static string FormatDateRange(DateOnly? startDate, DateOnly? endDate)
        => (startDate, endDate) switch
        {
            (null, null) => "Any dates",
            (null, not null) => $"{FormatDate(endDate.Value)}",
            (not null, null) => $"{FormatDate(startDate.Value)}",
            _ => $"{FormatDate(startDate!.Value)} - {FormatDate(endDate!.Value)}",
        };

    private static string FormatDateRangeOption(string value)
    {
        var (startDate, endDate) = ParseDateRange(value);
        return FormatDateRange(startDate, endDate);
    }

    private static (DateOnly? StartDate, DateOnly? EndDate) ParseDateRange(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (null, null);

        var dates = value.Split(" - ", StringSplitOptions.None);
        return (
            string.IsNullOrEmpty(dates[0])
                ? null
                : DateOnly.ParseExact(dates[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            string.IsNullOrEmpty(dates[1])
                ? null
                : DateOnly.ParseExact(dates[1], "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}