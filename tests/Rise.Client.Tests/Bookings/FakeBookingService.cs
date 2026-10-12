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

    /// <summary>
    /// what CreateCheckoutSessionAsync sends back, a Stripe checkout session by default
    /// </summary>
    public Result<BookingResponse.CheckoutSession> CheckoutAnswer { get; set; } = Result.Success(new BookingResponse.CheckoutSession
    {
        CheckoutUrl = "https://checkout.stripe.com/c/pay/cs_test_123",
        SessionId = "cs_test_123",
    });

    /// <summary>
    /// every checkout session the page asked for
    /// </summary>
    public List<BookingRequest.CreateCheckoutSession> CheckoutRequests { get; } = [];

    /// <summary>
    /// answers of GetDetailByCheckoutSessionAsync in order, the last one is repeated
    /// </summary>
    public Queue<Result<BookingDto.Detail>> DetailAnswers { get; } = new();

    /// <summary>
    /// nb of times the page loaded the booking of a checkout session
    /// </summary>
    public int DetailRequests { get; private set; }

    public Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(BookingRequest.CreateCheckoutSession request, CancellationToken cancellationToken = default)
    {
        CheckoutRequests.Add(request);
        return Task.FromResult(CheckoutAnswer);
    }

    public Task<Result<BookingDto.Detail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
        => throw new NotImplementedException();

    public Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(string checkoutSessionId, CancellationToken ctx = default)
    {
        DetailRequests++;
        var answer = DetailAnswers.Count > 1 ? DetailAnswers.Dequeue() : DetailAnswers.Peek();
        return Task.FromResult(answer);
    }
}
