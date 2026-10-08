namespace Rise.Shared.Bookings;

public interface IBookingService
{
    Task<Result<BookingResponse.Create>> CreateAsync(BookingRequest.Create request, CancellationToken ctx = default);
    Task<Result<BookingDto.Detail>> GetDetailAsync(int bookingId, CancellationToken ctx = default);
}