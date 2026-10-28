using Destructurama.Attributed;

namespace Rise.Shared.Bookings;

/// <summary>
/// Visual status of a single cell in the admin booking matrix.
/// </summary>
public enum CellStatus
{
    /// <summary>No booking covers this day — the room is available.</summary>
    Available,

    /// <summary>A paid/confirmed booking occupies this day.</summary>
    Occupied,

    /// <summary>A booking exists but payment has not yet been settled.</summary>
    Temporary,
}

public static class BookingDto
{
    public class Index
    {
        public required int Id { get; set; }
        public required string BookingNumber { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }
        public required string GuestName { get; set; }
        public required string GuestEmail { get; set; }
        public required decimal TotalAmount { get; set; }
        /// <summary>
        /// Room numbers of all rooms in this booking, e.g. ["101", "102"].
        /// </summary>
        public required List<string> RoomNumbers { get; set; }
        /// <summary>
        /// "Cancelled" for a cancelled booking, else "Paid" when at least one payment is settled, otherwise "Pending".
        /// </summary>
        public required string Status { get; set; }
    }

    public class Detail
    {
        public required int Id { get; set; }
        public required string BookingNumber { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }

        public required GuestDto Guest { get; set; }
        public required List<BookingRoomDto> Rooms { get; set; } = [];
        public required List<PaymentDto> Payments { get; set; } = [];
    }

    public class GuestDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
    }

    public class BookingRoomDto
    {
        public required int Id { get; set; }
        public required int RoomId { get; set; }
        public required string RoomNumber { get; set; }
        public required decimal PricePerNight { get; set; }
    }

    public class PaymentDto
    {
        public required int Id { get; set; }
        public required decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    /// <summary>
    /// Everything about one booking for the admin detail page: the booking, the guest, the stay and the payments.
    /// Holds private guest data, so only return it from admin endpoints.
    /// </summary>
    public class AdminDetail
    {
        public required int Id { get; set; }
        public required string BookingNumber { get; set; }
        public required DateTime CreatedAt { get; set; }
        /// <summary>
        /// "Cancelled" for a cancelled booking, else "Paid" when at least one payment is settled, otherwise "Pending".
        /// </summary>
        public required string Status { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }
        public required int Nights { get; set; }
        /// <summary>
        /// Nb of guests of the stay.
        /// </summary>
        public required int Adults { get; set; }
        /// <summary>
        /// Total of all rooms for the whole stay.
        /// </summary>
        public required decimal TotalAmount { get; set; }
        /// <summary>
        /// Sum of the settled payments, the most that can be refunded.
        /// </summary>
        public required decimal PaidAmount { get; set; }
        /// <summary>
        /// Refund due when cancelled today according to the cancellation policy copied at booking time,
        /// prefills the cancel form, never more than <see cref="PaidAmount"/>.
        /// </summary>
        public required decimal SuggestedRefundAmount { get; set; }
        /// <summary>
        /// Date the room refunds are calculated for: the cancellation date of a cancelled booking, otherwise today.
        /// </summary>
        public required DateOnly RefundReferenceDate { get; set; }
        /// <summary>
        /// Null while the booking is not cancelled.
        /// </summary>
        public AdminCancellationDto? Cancellation { get; set; }

        public required AdminGuestDto Guest { get; set; }
        public required List<AdminRoomDto> Rooms { get; set; } = [];
        public required List<AdminPaymentDto> Payments { get; set; } = [];
    }

    /// <summary>
    /// Contact details of the guest plus the language and party of the booking, null values are unknown.
    /// </summary>
    public class AdminGuestDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        [LogMasked]
        public required string Email { get; set; }
        [LogMasked]
        public required string Phone { get; set; }
        [LogMasked]
        public required string AddressLine1 { get; set; }
        [LogMasked]
        public string? AddressLine2 { get; set; }
        public required string Country { get; set; }
    }

    /// <summary>
    /// A room assigned to the booking with the rate, price and cancellation policy agreed at booking time.
    /// </summary>
    public class AdminRoomDto
    {
        public required int RoomId { get; set; }
        public required string RoomNumber { get; set; }
        public required string RoomName { get; set; }
        /// <summary>
        /// Name of the rate at booking time, a later rename doesn't change it.
        /// </summary>
        public required string RateName { get; set; }
        public required decimal PricePerNight { get; set; }
        /// <summary>
        /// Price of this room for all nights.
        /// </summary>
        public required decimal Total { get; set; }

        /// <summary>
        /// Cancellation policy copied at booking time, later changes to the rate don't apply.
        /// </summary>
        public required bool IsRefundable { get; set; }
        public required int FullRefundDays { get; set; }
        public required int PartialRefundDays { get; set; }
        public required int PartialRefundPercent { get; set; }
        /// <summary>
        /// Last day with a full refund, null for a non-refundable rate.
        /// </summary>
        public DateOnly? FullRefundUntil { get; set; }
        /// <summary>
        /// Last day with a partial refund, null for a non-refundable rate.
        /// </summary>
        public DateOnly? PartialRefundUntil { get; set; }
        /// <summary>
        /// Pct refunded on <see cref="AdminDetail.RefundReferenceDate"/>, eg 100, 50 or 0.
        /// </summary>
        public required int RefundPercent { get; set; }
        /// <summary>
        /// Refund of this room on <see cref="AdminDetail.RefundReferenceDate"/>.
        /// </summary>
        public required decimal RefundAmount { get; set; }
    }

    /// <summary>
    /// When and why the booking was cancelled and the refund the admin decided on.
    /// </summary>
    public class AdminCancellationDto
    {
        public required DateTime CancelledAt { get; set; }
        public required string Reason { get; set; }
        public required decimal RefundAmount { get; set; }
    }

    public class AdminPaymentDto
    {
        public required int Id { get; set; }
        public required decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? CardBrand { get; set; }
        public string? CardLast4 { get; set; }
        public string? StripeReceiptUrl { get; set; }
    }

    /// <summary>
    /// One room row in the admin booking matrix, with all bookings that overlap the requested window.
    /// </summary>
    public class MatrixRow
    {
        public required int RoomId { get; set; }
        public required string RoomNumber { get; set; }
        public required string RoomName { get; set; }
        public required List<MatrixBooking> Bookings { get; set; }
    }

    /// <summary>
    /// A single booking entry rendered as a coloured span inside the matrix grid.
    /// </summary>
    public class MatrixBooking
    {
        public required int Id { get; set; }
        public required string BookingNumber { get; set; }
        public required string GuestName { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }
        /// <summary>
        /// Visual cell status derived from the payment state of this booking.
        /// <see cref="CellStatus.Occupied"/> when at least one payment is settled,
        /// <see cref="CellStatus.Temporary"/> when the booking exists but is unpaid.
        /// </summary>
        public required CellStatus Status { get; set; }
    }
}
