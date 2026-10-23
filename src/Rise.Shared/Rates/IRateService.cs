using Rise.Shared.Common;

namespace Rise.Shared.Rates;

public interface IRateService
{
    Task<Result<RateResponse.Create>> CreateAsync(RateRequest.Create request, CancellationToken ctx = default);
    Task<Result<RateResponse.Index>> GetIndexAsync(RateRequest.Index request, CancellationToken ctx = default);
}