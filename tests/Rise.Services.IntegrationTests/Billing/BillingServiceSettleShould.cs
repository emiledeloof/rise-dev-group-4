using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Services.Billing;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Shared.Billing;

namespace Rise.Services.IntegrationTests.Billing;

[Collection(DatabaseCollection.Name)]
public class BillingServiceSettleShould(PostgresFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // 2 nights at 100 with 50 already paid, so 150 is still owed, returns the booking id
    private async Task<int> SeedAsync(Action<Booking>? tweak = null)
    {
        await using var dbContext = database.CreateDbContext();
        var room = new Room("101", "Standard Room", 2);
        var rate = room.AddRate("Flexible", 100m);
        var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261006-SVM-000042", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
        booking.AddRoom(room, rate);
        booking.AddPayment(50m).MarkAsPaid(new DateTime(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc));

        // lets a test change the booking before saving, for example cancel it
        tweak?.Invoke(booking);

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();
        return booking.Id;
    }

    private async Task<Result<BillingDto.Bill>> SettleAsync(int bookingId)
    {
        await using var dbContext = database.CreateDbContext();
        return await new BillingService(dbContext).SettleAsync(new BillingRequest.Settle { BookingId = bookingId });
    }

    private async Task<Booking> ReadBookingAsync(int bookingId)
    {
        await using var dbContext = database.CreateDbContext();
        return await dbContext.Bookings.Include(x => x.Payments).SingleAsync(x => x.Id == bookingId);
    }

    [Fact]
    public async Task CollectTheOutstandingBalanceAndCloseTheBill()
    {
        var bookingId = await SeedAsync();

        var result = await SettleAsync(bookingId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsSettled.ShouldBeTrue();
        result.Value.PaidAmount.ShouldBe(200m);
        result.Value.OutstandingBalance.ShouldBe(0m);

        var saved = await ReadBookingAsync(bookingId);
        saved.SettledAt.ShouldNotBeNull();
        saved.Payments.Count.ShouldBe(2);
        saved.Payments.ShouldContain(x => x.Amount == 150m && x.IsPaid);
    }

    // ! US-B11.7, the second call must not record a second payment
    [Fact]
    public async Task RefuseASecondSettlement()
    {
        var bookingId = await SeedAsync();
        await SettleAsync(bookingId);

        var result = await SettleAsync(bookingId);

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await ReadBookingAsync(bookingId)).Payments.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RefuseToSettleACancelledBooking()
    {
        var bookingId = await SeedAsync(booking => booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m));

        var result = await SettleAsync(bookingId);

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await ReadBookingAsync(bookingId)).IsSettled.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownBooking()
    {
        var result = await SettleAsync(42);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task RefuseNewLinesOnASettledBill()
    {
        var bookingId = await SeedAsync();
        await SettleAsync(bookingId);

        await using var dbContext = database.CreateDbContext();
        var result = await new BillingService(dbContext).AddLineAsync(new BillingRequest.AddLine
        {
            BookingId = bookingId,
            Description = "Minibar",
            Quantity = 1,
            UnitPrice = 8m,
        });

        result.Status.ShouldBe(ResultStatus.Conflict);
    }
}
