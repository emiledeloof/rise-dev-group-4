using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Persistence;
using Rise.Services.Invoice;
using Rise.Shared.Common;

namespace Rise.Services.Tests.Invoice;

public class InvoiceServiceShould
{
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName) // Do NOT use InMemoryDatabase... it's not reliable. Use a real database and come up with a strategy to clean up the database between tests.
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var guest = new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-0001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));

        var deposit = booking.AddPayment(100m, "cs_test_1");
        deposit.MarkAsPaid(new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc), "in_test_1", "INV-2026-0001");
        deposit.SetStripePaymentDetails("pi_test_1", "https://pay.stripe.com/receipts/test_1", "visa", "4242");
        booking.AddPayment(118m, "cs_test_2");

        dbContext.Guests.Add(guest);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return dbContext;
    }

    [Fact]
    public async Task ReturnPaymentsWithBookingNumberAndGuestName()
    {
        // Arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnPaymentsWithBookingNumberAndGuestName));
        var service = new InvoiceService(dbContext);

        // Act
        var result = await service.GetIndexAsync(new QueryRequest.SkipTake());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var invoices = result.Value.Invoices.ToList();
        invoices.Count.ShouldBe(2);
        invoices.ShouldAllBe(x => x.BookingNumber == "20261001-0001" && x.GuestName == "Jan Janssens");

        var paid = invoices.Single(x => x.IsPaid);
        paid.Amount.ShouldBe(100m);
        paid.InvoiceNumber.ShouldBe("INV-2026-0001");
    }

    [Fact]
    public async Task ReturnStripePaymentDetails()
    {
        // Arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnStripePaymentDetails));
        var service = new InvoiceService(dbContext);

        // Act
        var result = await service.GetIndexAsync(new QueryRequest.SkipTake());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var invoices = result.Value.Invoices.ToList();

        var paid = invoices.Single(x => x.IsPaid);
        paid.StripePaymentIntent.ShouldBe("pi_test_1");
        paid.StripeReceiptUrl.ShouldBe("https://pay.stripe.com/receipts/test_1");
        paid.CardBrand.ShouldBe("visa");
        paid.CardLast4.ShouldBe("4242");

        var open = invoices.Single(x => !x.IsPaid);
        open.StripePaymentIntent.ShouldBeNull();
        open.StripeReceiptUrl.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnTotalCountOfPayments()
    {
        // Arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTotalCountOfPayments));
        var service = new InvoiceService(dbContext);

        // Act
        var result = await service.GetIndexAsync(new QueryRequest.SkipTake { Take = 1 });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Invoices.Count().ShouldBe(1);
        result.Value.TotalCount.ShouldBe(2);
    }
}
