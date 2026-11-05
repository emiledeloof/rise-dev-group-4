using System.Globalization;

namespace Rise.Client.Common;

/// <summary>
/// formats shared by the admin pages, english until the languages task
/// </summary>
public static class Format
{
    /// <summary>
    /// eg €432.00
    /// </summary>
    public static string Amount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// eg 21 % or 5.5 %, no trailing zeros
    /// </summary>
    public static string Percent(decimal percentage)
        => $"{percentage.ToString("0.##", CultureInfo.InvariantCulture)} %";

    /// <summary>
    /// eg 02 Oct 2026
    /// </summary>
    public static string Date(DateOnly date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// eg 02 Oct 2026, the time is left out
    /// </summary>
    public static string Date(DateTime date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Stripe sends lowercase brands like "visa" or "mastercard", eg Visa •••• 4242
    /// </summary>
    public static string Card(string? brand, string last4)
    {
        var name = string.IsNullOrWhiteSpace(brand)
            ? "Card"
            : char.ToUpperInvariant(brand[0]) + brand[1..];
        return $"{name} •••• {last4}";
    }
}
