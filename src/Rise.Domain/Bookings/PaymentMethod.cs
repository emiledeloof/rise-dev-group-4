namespace Rise.Domain.Bookings;

/// <summary>
/// How the guest paid, <see cref="Online"/> goes through Stripe, the others are recorded by the admin on site.
/// </summary>
public enum PaymentMethod
{
    Online,
    Cash,
    CardTerminal,
    BankTransfer,
}
