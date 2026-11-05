namespace Rise.Shared.Billing;

public static class BillingDto
{
    /// <summary>
    /// The final bill of a booking. All amounts include VAT unless named net.
    /// Catalogue services and cash payments will be added as more lines and payments later.
    /// </summary>
    public class Bill
    {
        public required int BookingId { get; set; }
        public required string BookingNumber { get; set; }
        /// <summary>
        /// One of <see cref="Bookings.BookingStatus"/>.
        /// </summary>
        public required string Status { get; set; }
        public required string GuestName { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }
        public required int Nights { get; set; }

        public required List<AccommodationLine> Accommodation { get; set; } = [];
        /// <summary>
        /// Free lines the admin typed in for what the guest used, eg minibar.
        /// Not the extra services of the catalogue, those come later in their own list.
        /// </summary>
        public required List<OtherServiceLine> OtherServices { get; set; } = [];

        public required decimal AccommodationTotal { get; set; }
        public required decimal OtherServicesTotal { get; set; }
        /// <summary>
        /// The VAT included in <see cref="Total"/>, one group per VAT pct.
        /// </summary>
        public required List<VatGroup> VatGroups { get; set; } = [];
        public required decimal NetAmount { get; set; }
        public required decimal VatAmount { get; set; }
        public required decimal Total { get; set; }

        public required List<Payment> Payments { get; set; } = [];
        /// <summary>
        /// Sum of the settled payments, open Stripe sessions don't count.
        /// </summary>
        public required decimal PaidAmount { get; set; }
        /// <summary>
        /// What the guest still has to pay, negative when the guest paid too much, 0 for a cancelled booking.
        /// </summary>
        public required decimal OutstandingBalance { get; set; }

        /// <summary>
        /// Null while the booking is not cancelled.
        /// </summary>
        public Cancellation? Cancellation { get; set; }

        public bool IsCancelled => Cancellation is not null;
    }

    /// <summary>
    /// A booked room with the price agreed at booking time.
    /// </summary>
    public class AccommodationLine
    {
        public required string RoomNumber { get; set; }
        public required string RoomName { get; set; }
        public required string RateName { get; set; }
        public required int Nights { get; set; }
        public required decimal PricePerNight { get; set; }
        public required decimal VatPercentage { get; set; }
        public required decimal Total { get; set; }
    }

    /// <summary>
    /// A free line on the bill, see <see cref="Bill.OtherServices"/>.
    /// </summary>
    public class OtherServiceLine
    {
        public required int Id { get; set; }
        public required string Description { get; set; }
        public required int Quantity { get; set; }
        public required decimal UnitPrice { get; set; }
        public required decimal VatPercentage { get; set; }
        public required decimal Total { get; set; }
    }

    /// <summary>
    /// All lines with the same VAT pct, eg the rooms at 10 % or the minibar at 22 %.
    /// </summary>
    public class VatGroup
    {
        public required decimal VatPercentage { get; set; }
        public required decimal GrossAmount { get; set; }
        public required decimal NetAmount { get; set; }
        public required decimal VatAmount { get; set; }
    }

    public class Payment
    {
        public required int Id { get; set; }
        public required decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? CardBrand { get; set; }
        public string? CardLast4 { get; set; }
        public string? StripeReceiptUrl { get; set; }

        public bool IsPaid => PaidAt is not null;
    }

    public class Cancellation
    {
        public required DateTime CancelledAt { get; set; }
        public required string Reason { get; set; }
        public required decimal RefundAmount { get; set; }
    }
}
