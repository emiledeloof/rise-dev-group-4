using Ardalis.Result;
using Rise.Shared.RatePricing;

namespace Rise.Client.Rates;

/// <summary>
/// fake of the edit api for the admin rates page tests, keeps the requests instead of calling http
/// </summary>
public class FakeRatePricingService : IRatePricingService
{
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
}
