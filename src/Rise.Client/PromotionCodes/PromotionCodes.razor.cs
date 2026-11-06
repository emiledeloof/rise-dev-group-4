using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Shared.PromotionCodes;

namespace Rise.Client.PromotionCodes;

public partial class PromotionCodes : ComponentBase
{
    [Inject]
    public required IPromotionCodeService PromotionCodeService { get; set; }

    private bool IsCreateFormOpen { get; set; }
    private PromotionCodeRequest.Create _model = NewModel();
    private EditContext _editContext = new(NewModel());
    private string? _errorMessage;
    private bool _isSaving;

    private static PromotionCodeRequest.Create NewModel() => new();

    private void ToggleCreateForm()
    {
        if (IsCreateFormOpen)
        {
            CloseCreateForm();
            return;
        }

        OpenCreateForm();
    }

    private void OpenCreateForm()
    {
        _model = NewModel();
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += ClearApiFeedback;
        _errorMessage = null;
        IsCreateFormOpen = true;
    }

    private void ClearApiFeedback(object? sender, FieldChangedEventArgs e)
    {
        _errorMessage = null;
    }

    private void CloseCreateForm()
    {
        IsCreateFormOpen = false;
        _model = NewModel();
        _editContext = new EditContext(_model);
        _errorMessage = null;
    }

    private async Task SavePromotionCode()
    {
        if (_isSaving)
            return;

        _isSaving = true;
        _errorMessage = null;

        try
        {
            _model.Code = _model.Code.Trim();
            if (!string.IsNullOrWhiteSpace(_model.EmailRestriction))
            {
                _model.EmailRestriction = _model.EmailRestriction.Trim();
            }

            var result = await PromotionCodeService.CreateAsync(_model);

            if (result.IsSuccess)
            {
                CloseCreateForm();
                return;
            }

            _errorMessage = string.Join(" ", result.Errors);
        }
        finally
        {
            _isSaving = false;
        }
    }
}

