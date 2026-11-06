using Ardalis.Result;
using Rise.Shared.RatePricing;
using Rise.Shared.Settings;

namespace Rise.Client.Rates;

/// <summary>
/// fake of the edit api for the admin rates page tests, keeps the requests instead of calling http
/// </summary>
public class FakeRatePricingService : IRatePricingService
{
    public Result<GeneralSettingsDto> GeneralSettingsAnswer { get; set; } = Result.Success(new GeneralSettingsDto
    {
        IsVatEnabled = false,
        VatPercentage = 0m,
    });

    public List<GeneralSettingsRequest.Update> GeneralSettingsRequests { get; } = [];

    /// <summary>
    /// what the fake answers, success by default
    /// </summary>
    public Result EditAnswer { get; set; } = Result.Success();

    /// <summary>
    /// every edit the page sent, to check the values
    /// </summary>
    public List<RatePricingRequest.Edit> EditRequests { get; } = [];

    public Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default)
    {
        EditRequests.Add(request);
        return Task.FromResult(EditAnswer);
    }

    public Task<Result<GeneralSettingsDto>> GetGeneralSettingsAsync(CancellationToken ctx = default)
        => Task.FromResult(GeneralSettingsAnswer);

    public Task<Result<GeneralSettingsDto>> UpdateGeneralSettingsAsync(
        GeneralSettingsRequest.Update request,
        CancellationToken ctx = default)
    {
        GeneralSettingsRequests.Add(request);
        GeneralSettingsAnswer = Result.Success(new GeneralSettingsDto
        {
            IsVatEnabled = request.IsVatEnabled,
            VatPercentage = request.VatPercentage,
        });
        return Task.FromResult(GeneralSettingsAnswer);
    }
}
