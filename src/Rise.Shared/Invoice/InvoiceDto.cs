namespace Rise.Shared.Invoice;

public static class InvoiceDto
{
    public class Index
    {
        public required int Id { get; set; }
        /// <summary>
        /// Used to link the invoice to the booking detail page.
        /// </summary>
        public required int BookingId { get; set; }
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
        /// <summary>
        /// Stripe PaymentIntent (pi_...), used to open the payment in the Stripe dashboard.
        /// </summary>
        public string? StripePaymentIntent { get; set; }
        /// <summary>
        /// Stripe's hosted payment receipt.
        /// </summary>
        public string? StripeReceiptUrl { get; set; }
        public string? CardBrand { get; set; }
        public string? CardLast4 { get; set; }
        /// <summary>
        /// Nb of rooms of the booking, cancelled ones included.
        /// </summary>
        public required int RoomCount { get; set; }
        public required int CancelledRoomCount { get; set; }
        /// <summary>
        /// Sum of the refunds on the whole booking, the amount of this payment stays what was paid.
        /// </summary>
        public required decimal RefundedAmount { get; set; }

        /// <summary>the final bill of the booking is closed</summary>
        public bool IsBillSettled { get; set; }

        public bool IsPaid => PaidAt is not null;
        public bool IsBookingCancelled => RoomCount > 0 && CancelledRoomCount == RoomCount;
    }
}
