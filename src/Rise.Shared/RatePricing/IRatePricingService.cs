using Rise.Shared.Settings;

namespace Rise.Shared.RatePricing;

/// <summary>
/// contract for the edit form of the admin rates page, the server implements it with the db, the client with HttpClient
/// </summary>
public interface IRatePricingService
{
    /// <summary>
    /// saves the pricing of one rate
    /// </summary>
    // * Result without data, the page only needs to know success or error
    Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default);
    Task<Result<GeneralSettingsDto>> GetGeneralSettingsAsync(CancellationToken ctx = default);
    Task<Result<GeneralSettingsDto>> UpdateGeneralSettingsAsync(
        GeneralSettingsRequest.Update request,
        CancellationToken ctx = default);
}
