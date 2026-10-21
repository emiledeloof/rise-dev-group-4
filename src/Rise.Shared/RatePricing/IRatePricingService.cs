namespace Rise.Shared.RatePricing;

/// <summary>
/// contract for the admin pricing page, the server implements it with the db, the client with HttpClient
/// </summary>
public interface IRatePricingService
{
    // GetIndexAsync, all rates with their pricing
    // returns Task of Result of RatePricingResponse.Index, takes a CancellationToken ctx = default
    Task<Result<RatePricingResponse.Index>> GetIndexAsync(CancellationToken ctx = default);

    // EditAsync, saves the new price of one rate
    // takes a RatePricingRequest.Edit and a CancellationToken ctx = default, returns Task of Result
    Task<Result> EditAsync(RatePricingRequest.Edit request, CancellationToken ctx = default);
}
