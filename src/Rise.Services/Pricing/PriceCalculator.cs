namespace Rise.Services.Pricing;

/// <summary>
/// price rules of a stay, one place so promos and discounts can be added here later
/// </summary>
public static class PriceCalculator
{
    /// <summary>
    /// total price of a room for the whole stay, eg 160 per night for 3 nights gives 480
    /// </summary>
    /// <param name="pricePerNight">price of 1 night, from the room rate</param>
    /// <param name="nights">nb of nights of the stay, at least 1</param>
    public static decimal Total(decimal pricePerNight, int nights)
    {
        // a stay always has at least 1 night, the StayPeriod validator checks it before
        // fail loudly if it's ever called with 0 or less, a wrong price is worse than a crash
        if (nights <= 0) throw new ArgumentOutOfRangeException(nameof(nights), "A stay has at least 1 night.");

        // price of 1 night times the nb of nights
        return pricePerNight * nights;
    }

    /// <summary>
    /// Applies VAT to a nightly price and rounds the result to the currency precision.
    /// </summary>
    public static decimal InclusiveOfVat(decimal price, bool isVatEnabled, decimal vatPercentage)
        => isVatEnabled && vatPercentage > 0m
            ? Math.Round(price * (1m + vatPercentage / 100m), 2, MidpointRounding.AwayFromZero)
            : price;
}
