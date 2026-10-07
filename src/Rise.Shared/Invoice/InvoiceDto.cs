namespace Rise.Shared.Invoice;

public static class InvoiceDto
{
    public class Index
    {
        public required int Id { get; set; }
        public required string BookingNumber { get; set; }
        /// <summary>
        /// Name of the guest who made the booking.
        /// </summary>
        public required string GuestName { get; set; }
        public required decimal Amount { get; set; }
        /// <summary>
        /// Used to download the Stripe invoice.
        /// </summary>
        public string? StripeInvoice { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? PaidAt { get; set; }

        public bool IsPaid => PaidAt is not null;
    }
}
