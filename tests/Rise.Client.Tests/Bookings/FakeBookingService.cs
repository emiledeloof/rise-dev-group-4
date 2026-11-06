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

    /// <summary>
    /// what GetIndexAsync sends back — 3 bookings by default
    /// </summary>
    public Result<BookingResponse.Index> IndexAnswer { get; set; } = Result.Success(new BookingResponse.Index
    {
        Bookings = Enumerable.Range(1, 3).Select(i => new BookingDto.Index
        {
            Id = i,
            BookingNumber = $"20261001-SVM-00000{i}",
            GuestName = $"Guest {i}",
            GuestEmail = $"guest{i}@example.com",
            CheckIn = new DateOnly(2026, 10, i + 1),
            CheckOut = new DateOnly(2026, 10, i + 3),
            TotalAmount = i * 100m,
            RoomNumbers = [$"10{i}"],
            Status = i == 1 ? "Paid" : "Pending",
            RoomCount = 1,
            CancelledRoomCount = 0,
        }).ToList(),
        TotalCount = 3,
    });

    public Task<Result<BookingResponse.CheckoutSession>> CreateCheckoutSessionAsync(BookingRequest.CreateCheckoutSession request, CancellationToken cancellationToken = default)
    {
        CheckoutRequests.Add(request);
        return Task.FromResult(CheckoutAnswer);
    }

    /// <summary>
    /// what GetDetailAsync sends back, the booking of <see cref="TestAdminDetails.Full"/> by default
    /// </summary>
    public Result<BookingDto.AdminDetail> AdminDetailAnswer { get; set; } = Result.Success(TestAdminDetails.Full());

    public Task<Result<BookingDto.AdminDetail>> GetDetailAsync(int bookingId, CancellationToken ctx = default)
        => Task.FromResult(AdminDetailAnswer);

    public Task<Result<BookingDto.Detail>> GetDetailByCheckoutSessionAsync(string checkoutSessionId, CancellationToken ctx = default)
    {
        DetailRequests++;
        var answer = DetailAnswers.Count > 1 ? DetailAnswers.Dequeue() : DetailAnswers.Peek();
        return Task.FromResult(answer);
    }

    /// <summary>
    /// every list page the page asked for
    /// </summary>
    public List<BookingRequest.Index> IndexRequests { get; } = [];

    /// <summary>
    /// answers of GetIndexAsync that a test completes itself, used up first, then <see cref="IndexAnswer"/> is sent back
    /// </summary>
    public Queue<Task<Result<BookingResponse.Index>>> PendingIndexAnswers { get; } = new();

    public Task<Result<BookingResponse.Index>> GetIndexAsync(BookingRequest.Index request, CancellationToken ctx = default)
    {
        IndexRequests.Add(request);
        return PendingIndexAnswers.Count > 0 ? PendingIndexAnswers.Dequeue() : Task.FromResult(IndexAnswer);
    }

    /// <summary>
    /// what CancelAsync sends back, all rooms cancelled with a refund of 360 by default
    /// </summary>
    public Result<BookingResponse.Cancel> CancelAnswer { get; set; } = Result.Success(new BookingResponse.Cancel
    {
        BookingId = 7,
        BookingNumber = "20261004-SVM-000007",
        CancelledAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
        RefundAmount = 360m,
        CancelledRoomCount = 2,
        RemainingRoomCount = 0,
    });

    /// <summary>
    /// every cancellation the page sent
    /// </summary>
    public List<BookingRequest.Cancel> CancelRequests { get; } = [];

    public Task<Result<BookingResponse.Cancel>> CancelAsync(BookingRequest.Cancel request, CancellationToken ctx = default)
    {
        CancelRequests.Add(request);
        return Task.FromResult(CancelAnswer);
    }

    public Result<BookingResponse.Matrix>? MatrixAnswer { get; set; }

    public Task<Result<BookingResponse.Matrix>> GetMatrixAsync(BookingRequest.Matrix request, CancellationToken ctx = default)
        => Task.FromResult(MatrixAnswer ?? Result.Success(new BookingResponse.Matrix
        {
            WindowStart = request.WindowStart,
            WindowDays = request.WindowDays,
            Rows = [],
        }));
}
