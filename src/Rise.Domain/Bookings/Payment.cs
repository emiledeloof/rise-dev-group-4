namespace Rise.Domain.Bookings;

/// <summary>
/// A (Stripe) payment for a <see cref="Booking"/>, created via <see cref="Booking.AddPayment"/>.
/// </summary>
public class Payment : Entity
{
    public Booking Booking { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string? StripeCheckoutSession { get; private set; }
    /// <summary>
    /// Used to download the Stripe invoice.
    /// </summary>
    public string? StripeInvoice { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public bool IsPaid => PaidAt is not null;

    /// <summary>
    /// Entity Framework Core Constructor
    /// </summary>
    private Payment()
    {
    }

    internal Payment(Booking booking, decimal amount, string? stripeCheckoutSession)
    {
        Booking = Guard.Against.Null(booking);
        Amount = Guard.Against.NegativeOrZero(amount);
        StripeCheckoutSession = stripeCheckoutSession;
    }

    public void MarkAsPaid(DateTime paidAt, string? stripeInvoice = null, string? invoiceNumber = null)
    {
        if (IsPaid)
            throw new InvalidOperationException("The payment is already paid.");

        PaidAt = paidAt;
        StripeInvoice = stripeInvoice;
        InvoiceNumber = invoiceNumber;
    }
}
