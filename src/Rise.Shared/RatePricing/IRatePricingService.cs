namespace Rise.Shared.RatePricing;

/// <summary>
/// contract for the admin pricing page, the server implements it with the db, the client with HttpClient
/// </summary>
public interface IRatePricingService
{
    /// <summary>
    /// all rates with their pricing
    /// </summary>
    Task<Result<RatePricingResponse.Index>> GetIndexAsync(CancellationToken ctx = default);

    /// <summary>
    /// saves the pricing of one rate
    /// </summary>
    // * Result without data, the page only needs to know success or error
    Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default);
}
