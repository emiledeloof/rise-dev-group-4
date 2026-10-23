using Destructurama.Attributed;

namespace Rise.Shared.Bookings;

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
        /// Derived payment status: "Paid" when at least one payment is settled, otherwise "Pending".
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
        /// Derived payment status: "Paid" when at least one payment is settled, otherwise "Pending".
        /// </summary>
        public required string Status { get; set; }
        public required DateOnly CheckIn { get; set; }
        public required DateOnly CheckOut { get; set; }
        public required int Nights { get; set; }
        /// <summary>
        /// Total of all rooms for the whole stay.
        /// </summary>
        public required decimal TotalAmount { get; set; }

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
    /// A room assigned to the booking with the rate and price of the stay.
    /// </summary>
    public class AdminRoomDto
    {
        public required int RoomId { get; set; }
        public required string RoomNumber { get; set; }
        public required string RoomName { get; set; }
        public required string RateName { get; set; }
        public required decimal PricePerNight { get; set; }
        /// <summary>
        /// Price of this room for all nights.
        /// </summary>
        public required decimal Total { get; set; }
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
        /// "Paid" when at least one payment is settled, otherwise "Pending".
        /// </summary>
        public required string Status { get; set; }
    }
}
