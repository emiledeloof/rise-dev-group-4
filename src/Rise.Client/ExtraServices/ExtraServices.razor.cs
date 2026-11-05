using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.ExtraServices;

namespace Rise.Client.ExtraServices;

/// <summary>
/// Code-behind for the admin extra services management page.
/// </summary>
public partial class ExtraServices : ComponentBase
{
    private IReadOnlyList<ExtraServiceDto.Index>? _services;
    private bool _loadFailed;

    private string? _message;
    private bool _messageIsError;

    private ExtraServiceRequest.Form? _formModel;
    private string? _formError;
    private bool _isSaving;

    [Inject] public required IExtraServiceService ExtraServiceService { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var result = await ExtraServiceService.GetIndexAsync();
        _loadFailed = !result.IsSuccess;

        if (_loadFailed)
        {
            ShowMessage("The extra services could not be loaded.", isError: true);
            _services = [];
            return;
        }

        _services = result.Value.Services.ToList();
    }

    private void OpenCreateForm()
    {
        _formError = null;
        _formModel = new ExtraServiceRequest.Create
        {
            Price = 15m,
            Cadence = ChargeCadence.PerNight,
            IsPerPerson = true
        };
    }

    private void OpenEditForm(ExtraServiceDto.Index service)
    {
        _formError = null;
        _formModel = new ExtraServiceRequest.Edit
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            Cadence = service.Cadence,
            IsPerPerson = service.IsPerPerson,
            IsActive = service.IsActive
        };
    }

    private void CloseForm()
    {
        _formModel = null;
        _formError = null;
    }

    private async Task SaveAsync()
    {
        _isSaving = true;
        _formError = null;

        try
        {
            Ardalis.Result.IResult result = _formModel switch
            {
                ExtraServiceRequest.Edit edit => await ExtraServiceService.EditAsync(edit),
                ExtraServiceRequest.Create create => await ExtraServiceService.CreateAsync(create),
                _ => throw new InvalidOperationException("The form has no model.")
            };

            if (result.Status != ResultStatus.Ok)
            {
                _formError = FirstError(result, "The extra service could not be saved.");
                return;
            }

            var savedName = _formModel.Name;
            var isEdit = _formModel is ExtraServiceRequest.Edit;

            CloseForm();
            await LoadAsync();

            ShowMessage(isEdit
                ? $"Service '{savedName}' was updated."
                : $"Service '{savedName}' was created.");
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task ToggleActiveAsync(ExtraServiceDto.Index service)
    {
        var result = await ExtraServiceService.ToggleActiveAsync(service.Id);

        if (!result.IsSuccess)
        {
            ShowMessage(FirstError(result, "Could not update service status."), isError: true);
            return;
        }

        await LoadAsync();
        ShowMessage(service.IsActive
            ? $"Service '{service.Name}' was deactivated."
            : $"Service '{service.Name}' was activated.");
    }

    private void ShowMessage(string message, bool isError = false)
    {
        _message = message;
        _messageIsError = isError;
    }

    private static string FirstError(Ardalis.Result.IResult result, string fallback)
    {
        if (result.Errors.Any())
            return result.Errors.First();

        if (result.ValidationErrors.Any())
            return result.ValidationErrors.First().ErrorMessage;

        return fallback;
    }

    private static string FormatPrice(decimal price)
        => price.ToString("C2", CultureInfo.GetCultureInfo("nl-BE"));

    private static string FormatPricingModel(ChargeCadence cadence, bool isPerPerson)
    {
        var targetText = isPerPerson ? "per person" : "per booking";
        var cadenceText = cadence == ChargeCadence.PerNight ? "per night" : "once";

        return $"· {targetText} · {cadenceText}";
    }
}
