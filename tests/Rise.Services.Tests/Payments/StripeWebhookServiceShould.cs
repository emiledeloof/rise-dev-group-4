using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
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
        var doubleRoom = new Room("102", "Double Room", 2);
        var singleRoom = new Room("101", "Single Room", 1);
        booking.AddRoom(doubleRoom, doubleRoom.AddRate("Flexible", 30m));
        booking.AddRoom(singleRoom, singleRoom.AddRate("Flexible", 20m));
        var payment = booking.AddPayment(100m, SessionId);

        dbContext.Rooms.AddRange(doubleRoom, singleRoom);
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

    /// <summary>
    /// ! the webhook runs in a fresh DbContext like in production, the shared one of the seed would hide a missing Include of the rooms,
    /// without them a cancelled booking looks active and the warning for the admin is skipped
    /// </summary>
    [Theory]
    [InlineData(1, false, true)]
    [InlineData(2, true, false)]
    public async Task MarkPaymentAsPaidAfterRoomsWereCancelled(int cancelledRooms, bool expectedCancelled, bool expectedPartial)
    {
        // Arrange
        var databaseName = $"{nameof(MarkPaymentAsPaidAfterRoomsWereCancelled)}-{cancelledRooms}";
        var (seedContext, seededPayment) = await CreateSeededDbContextAsync(databaseName);
        var paymentId = seededPayment.Id;
        await using (seedContext)
        {
            var booking = seededPayment.Booking;
            booking.CancelRooms(
                booking.Rooms.OrderBy(x => x.Room.MaxGuests).Take(cancelledRooms).Select(x => new RoomCancellation(x, 0m)).ToList(),
                SessionCreated,
                "Guest asked to cancel by phone");
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);
        var service = new StripeWebhookService(dbContext, paymentGateway);

        // Act
        var result = await service.HandleCheckoutSessionCompletedAsync(
            new StripeCheckoutSessionCompleted(SessionId, paymentId, ChargeCreated));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var payment = dbContext.ChangeTracker.Entries<Payment>().Single().Entity;
        payment.IsPaid.ShouldBeTrue();
        payment.Booking.IsCancelled.ShouldBe(expectedCancelled);
        payment.Booking.IsPartiallyCancelled.ShouldBe(expectedPartial);
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
