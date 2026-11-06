namespace Rise.Domain.Bookings;

/// <summary>
/// A room to cancel and the refund recorded for it, passed to <see cref="Booking.CancelRooms"/>.
/// </summary>
public sealed record RoomCancellation(BookingRoom Room, decimal RefundAmount);
