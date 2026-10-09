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
    /// <summary>
    /// The Stripe PaymentIntent (pi_...), links the payment to the Stripe dashboard.
    /// </summary>
    public string? StripePaymentIntent { get; private set; }
    /// <summary>
    /// Stripe's hosted payment receipt, the payment confirmation shown to the customer.
    /// </summary>
    public string? StripeReceiptUrl { get; private set; }
    public string? CardBrand { get; private set; }
    public string? CardLast4 { get; private set; }

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

    public void SetStripeCheckoutSession(string stripeCheckoutSession)
    {
        if (string.IsNullOrWhiteSpace(stripeCheckoutSession))
            throw new ArgumentException("A Stripe Checkout Session is required.", nameof(stripeCheckoutSession));

        if (!string.IsNullOrWhiteSpace(StripeCheckoutSession) &&
            StripeCheckoutSession != stripeCheckoutSession)
            throw new InvalidOperationException("The payment already has a different Stripe Checkout Session.");

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

    /// <summary>
    /// Stores the payment details Stripe returned for the paid Checkout Session.
    /// </summary>
    public void SetStripePaymentDetails(string paymentIntent, string? receiptUrl, string? cardBrand, string? cardLast4)
    {
        StripePaymentIntent = Guard.Against.NullOrWhiteSpace(paymentIntent);
        StripeReceiptUrl = receiptUrl;
        CardBrand = cardBrand;
        CardLast4 = cardLast4;
    }
}
