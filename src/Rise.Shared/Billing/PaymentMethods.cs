namespace Rise.Shared.Billing;

/// <summary>
/// How a payment was made, the names match the PaymentMethod enum of the domain.
/// Online payments go through Stripe, the admin records the <see cref="OnSite"/> ones on the bill.
/// </summary>
public static class PaymentMethods
{
    public const string Online = "Online";
    public const string Cash = "Cash";
    public const string CardTerminal = "CardTerminal";
    public const string BankTransfer = "BankTransfer";

    /// <summary>
    /// The methods the admin can pick when recording a payment, in the order of the form.
    /// </summary>
    public static readonly string[] OnSite = [Cash, CardTerminal, BankTransfer];

    public static bool IsOnSite(string method) => OnSite.Contains(method);

    public static string Label(string method) => method switch
    {
        Online => "Online · Stripe",
        Cash => "Cash",
        CardTerminal => "Card terminal",
        BankTransfer => "Bank transfer",
        _ => method,
    };
}
