namespace Rise.Shared.RatePricing;

/// <summary>
/// responses sent by the rate pricing endpoints
/// </summary>
public static partial class RatePricingResponse
{
    /// <summary>
    /// what the list endpoint sends back, all rates with their pricing
    /// </summary>
    public class Index
    {
        // * empty list by default so it's never null
        public IEnumerable<RatePricingDto.Index> Rates { get; set; } = [];
    }
}
