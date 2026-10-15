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
}