using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Persistence;
using Rise.Services.Payments;
using Stripe;

namespace Rise.Services.Tests.Payments;

public class StripeWebhookServiceShould
{
    private const string SessionId = "cs_test_1";
    private static readonly DateTime SessionCreated = new(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ChargeCreated = new(2026, 10, 1, 14, 5, 0, DateTimeKind.Utc);

    private readonly IStripePaymentGateway paymentGateway = Substitute.For<IStripePaymentGateway>();

    private static async Task<(ApplicationDbContext DbContext, Payment Payment)> CreateSeededDbContextAsync(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName) // Do NOT use InMemoryDatabase... it's not reliable. Use a real database and come up with a strategy to clean up the database between tests.
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var guest = new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-0001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
        var payment = booking.AddPayment(100m, SessionId);

        dbContext.Guests.Add(guest);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return (dbContext, payment);
    }

    [Fact]
    public async Task StoreStripePaymentDetails()
    {
        // Arrange
        var (dbContext, payment) = await CreateSeededDbContextAsync(nameof(StoreStripePaymentDetails));
        await using var _ = dbContext;
        paymentGateway.GetPaymentDetailsAsync(SessionId, Arg.Any<CancellationToken>())
            .Returns(new StripePaymentDetails("pi_test_1", "https://pay.stripe.com/receipts/test_1", "visa", "4242", ChargeCreated));
        var service = new StripeWebhookService(dbContext, paymentGateway);

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
        var service = new StripeWebhookService(dbContext, paymentGateway);

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
        var service = new StripeWebhookService(dbContext, paymentGateway);

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
        var service = new StripeWebhookService(dbContext, paymentGateway);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, payment.Id, SessionCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await paymentGateway.DidNotReceiveWithAnyArgs().GetPaymentDetailsAsync(default!, default);
    }
}
