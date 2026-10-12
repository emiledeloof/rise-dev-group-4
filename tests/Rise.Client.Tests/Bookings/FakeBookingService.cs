using Ardalis.Result;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// fake IBookingService for the page tests, no http call
/// each test chooses what CreateAsync sends back
/// </summary>
public class FakeBookingService : IBookingService
{
    /// <summary>
    /// what CreateAsync sends back, a saved booking by default
    /// </summary>
    public Result<BookingResponse.Create> CreateAnswer { get; set; } = Result.Created(new BookingResponse.Create
    {
        BookingId = 1,
        BookingNumber = "20261012-SVM-000042",
        TotalAmount = 327m,
    });

    /// <summary>
    /// every booking the page tried to save
    /// </summary>
    public List<BookingRequest.Create> CreateRequests { get; } = [];

    public Task<Result<BookingResponse.Create>> CreateAsync(BookingRequest.Create request, CancellationToken ctx = default)
    {
        CreateRequests.Add(request);
        return Task.FromResult(CreateAnswer);
    }

    public Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(BookingRequest.CreateCheckoutSession request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Result<BookingDto.Detail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
        => throw new NotImplementedException();

    public Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(string checkoutSessionId, CancellationToken ctx = default)
        => throw new NotImplementedException();
}
