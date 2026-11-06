using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Payments;
using Rise.Shared.Email;
using Stripe;

namespace Rise.Services.Tests.Payments;

public class StripeWebhookServiceShould
{
    private const string SessionId = "cs_test_1";
    private static readonly DateTime SessionCreated = new(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ChargeCreated = new(2026, 10, 1, 14, 5, 0, DateTimeKind.Utc);

    private readonly IStripePaymentGateway paymentGateway = Substitute.For<IStripePaymentGateway>();
    private readonly IEmailService emailService = Substitute.For<IEmailService>();

    private static async Task<(ApplicationDbContext DbContext, Payment Payment)> CreateSeededDbContextAsync(string databaseName, bool withRoom = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName) // Do NOT use InMemoryDatabase... it's not reliable. Use a real database and come up with a strategy to clean up the database between tests.
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var guest = new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-0001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);

        if (withRoom)
        {
            var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
            var rate = room.AddRate("Standard", 79m);
            booking.AddRoom(room, rate);
            dbContext.Rooms.Add(room);
        }

        var payment = booking.AddPayment(withRoom ? 158m : 100m, SessionId);

        dbContext.Guests.Add(guest);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return (dbContext, payment);
    }

    private StripeWebhookService CreateService(ApplicationDbContext dbContext)
        => new(dbContext, paymentGateway, emailService);

    [Fact]
    public async Task StoreStripePaymentDetails()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(StoreStripePaymentDetails));
        await using var _ = dbContext;
        paymentGateway.GetPaymentDetailsAsync(SessionId, Arg.Any<CancellationToken>())
            .Returns(new StripePaymentDetails("pi_test_1", "https://pay.stripe.com/receipts/test_1", "visa", "4242", ChargeCreated));
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.IsPaid.ShouldBeTrue();
        payment.PaidAt.ShouldBe(ChargeCreated);
        payment.StripePaymentIntent.ShouldBe("pi_test_1");
        payment.StripeReceiptUrl.ShouldBe("https://pay.stripe.com/receipts/test_1");
        payment.CardBrand.ShouldBe("visa");
        payment.CardLast4.ShouldBe("4242");
    }

    [Fact]
    public async Task MarkAsPaidWhenStripeDetailsCannotBeLoaded()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(MarkAsPaidWhenStripeDetailsCannotBeLoaded));
        await using var _ = dbContext;
        paymentGateway.GetPaymentDetailsAsync(SessionId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new StripeException("Stripe is unreachable."));
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.IsPaid.ShouldBeTrue();
        payment.PaidAt.ShouldBe(SessionCreated);
        payment.StripePaymentIntent.ShouldBeNull();
    }

    [Fact]
    public async Task MarkPaymentOfCancelledBookingAsPaid()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(MarkPaymentOfCancelledBookingAsPaid));
        await using var _ = dbContext;
        payment.Booking.Cancel(SessionCreated, "Guest asked to cancel by phone", 0m);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, ChargeCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.IsPaid.ShouldBeTrue();
        payment.Booking.IsCancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task NotLoadStripeDetailsWhenAlreadyPaid()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(NotLoadStripeDetailsWhenAlreadyPaid));
        await using var _ = dbContext;
        payment.MarkAsPaid(SessionCreated);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await paymentGateway.DidNotReceiveWithAnyArgs().GetPaymentDetailsAsync(default!, default);
    }

    // ── Email confirmation ─────────────────────────────────────────────────────

    [Fact]
    public async Task SendConfirmationEmailWhenPaymentSucceeds()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(SendConfirmationEmailWhenPaymentSucceeds), withRoom: true);
        await using var _ = dbContext;
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert — payment succeeded and one confirmation email was sent
        result.IsSuccess.ShouldBeTrue();
        await emailService.Received(1).SendBookingConfirmationAsync(
            Arg.Is<BookingConfirmationEmail>(e => e.BookingNumber == "20261001-0001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendConfirmationEmailWithCorrectGuestAndRoomData()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(SendConfirmationEmailWithCorrectGuestAndRoomData), withRoom: true);
        await using var _ = dbContext;
        var service = CreateService(dbContext);

        // Act
        await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert the DTO fields passed to the email service
        await emailService.Received(1).SendBookingConfirmationAsync(
            Arg.Is<BookingConfirmationEmail>(e =>
                e.GuestName == "Jan Janssens" &&
                e.GuestEmail == "jan.janssens@example.com" &&
                e.CheckIn == new DateOnly(2026, 10, 10) &&
                e.CheckOut == new DateOnly(2026, 10, 12) &&
                e.Nights == 2 &&
                e.Adults == 2 &&
                e.TotalAmount == 158m &&
                e.Rooms.Count == 1 &&
                e.Rooms[0].RoomNumber == "101" &&
                e.Rooms[0].RoomName == "Single Room" &&
                e.Rooms[0].RateName == "Standard" &&
                e.Rooms[0].PricePerNight == 79m &&
                e.Rooms[0].Total == 158m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotSendEmailWhenPaymentWasAlreadyPaid()
    {
        // Arrange — the payment is already paid (idempotency path returns early)
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(NotSendEmailWhenPaymentWasAlreadyPaid), withRoom: true);
        await using var _ = dbContext;
        payment.MarkAsPaid(SessionCreated);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert — early return means no email
        result.IsSuccess.ShouldBeTrue();
        await emailService.DidNotReceiveWithAnyArgs().SendBookingConfirmationAsync(default!, default);
    }

    [Fact]
    public async Task NotFailWebhookWhenEmailServiceThrows()
    {
        // Arrange — the email service throws a transient error
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(NotFailWebhookWhenEmailServiceThrows), withRoom: true);
        await using var _ = dbContext;
        emailService
            .SendBookingConfirmationAsync(Arg.Any<BookingConfirmationEmail>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP server unreachable."));
        var service = CreateService(dbContext);

        // Act — must not throw
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert — the payment is still marked as paid, the webhook returns success
        result.IsSuccess.ShouldBeTrue();
        payment.IsPaid.ShouldBeTrue();
    }
}
