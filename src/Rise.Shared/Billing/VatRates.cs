namespace Rise.Shared.Billing;

/// <summary>
/// VAT pcts the admin can pick for a free bill line, the Italian rates, the guesthouse is in Sardinia.
/// The services catalogue can bring its own rates later.
/// </summary>
public static class VatRates
{
    public static readonly decimal[] Presets = [0m, 4m, 5m, 10m, 22m];

    /// <summary>
    /// Standard rate, eg minibar drinks or an excursion.
    /// </summary>
    public const decimal Default = 22m;
}
