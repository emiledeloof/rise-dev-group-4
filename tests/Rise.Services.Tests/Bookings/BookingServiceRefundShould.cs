using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Services.Payments;
using Rise.Shared.Bookings;

namespace Rise.Services.Tests.Bookings;

/// <summary>
/// tests for recording and undoing the refund payments of cancelled rooms
/// the booking is for 2 nights, 300 paid: room 301 (100 a night, total 200) and room 302 (50 a night, total 100)
/// </summary>
public class BookingServiceRefundShould
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record Seed(BookingService Service, int BookingId, int FirstId, int SecondId);

    /// <summary>
    /// cancels room 301 with the given refund and room 302 with refund 0 when asked
    /// </summary>
    private static async Task<Seed> SeedAsync(string databaseName, decimal refund = 150m, bool cancelSecond = false, decimal paid = 300m)
    {
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.GeneralSettings.Add(new GeneralSettings(false, 0m));

            var first = new Room("301", "Deluxe Room", 2, new RoomType("Standard"));
            var second = new Room("302", "Single Room", 1, new RoomType("Standard"));
            var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));
            var checkIn = Today.AddDays(60);
            var booking = new Booking("20261001-SVM-000001", guest, checkIn, checkIn.AddDays(2), 1);
            var a = booking.AddRoom(first, first.AddRate("Flexible", 100m));
            var b = booking.AddRoom(second, second.AddRate("Flexible", 50m));
            booking.AddPayment(paid).MarkAsPaid(DateTime.UtcNow.AddDays(-1));

            var cancellations = new List<RoomCancellation> { new(a, refund) };
            if (cancelSecond)
                cancellations.Add(new RoomCancellation(b, 0m));
            booking.CancelRooms(cancellations, DateTime.UtcNow, "Guest is ill");

            seedContext.Rooms.AddRange(first, second);
            seedContext.Guests.Add(guest);
            seedContext.Bookings.Add(booking);
            await seedContext.SaveChangesAsync();
        }

        var dbContext = CreateContext(databaseName);
        var rooms = await dbContext.BookingRooms.AsNoTracking().Include(x => x.Room).ToListAsync();
        var service = new BookingService(dbContext, Substitute.For<IStripePaymentGateway>(), Substitute.For<IConfiguration>());

        return new Seed(
            service,
            (await dbContext.Bookings.AsNoTracking().SingleAsync()).Id,
            rooms.Single(x => x.Room.RoomNumber == "301").Id,
            rooms.Single(x => x.Room.RoomNumber == "302").Id);
    }

    private static ApplicationDbContext CreateContext(string databaseName)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options);

    private static async Task<BookingRoom> StoredRoomAsync(string databaseName, int bookingRoomId)
    {
        await using var dbContext = CreateContext(databaseName);
        return await dbContext.BookingRooms.AsNoTracking().SingleAsync(x => x.Id == bookingRoomId);
    }

    private static BookingRequest.RecordRefund Request(Seed seed, decimal amount, int? bookingRoomId = null, DateOnly? paidOn = null) => new()
    {
        BookingId = seed.BookingId,
        BookingRoomId = bookingRoomId ?? seed.FirstId,
        PaidOn = paidOn ?? Today,
        Amount = amount,
        Note = "Stripe re_123",
    };

    [Fact]
    public async Task RecordThePaidRefund()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);

        var result = await seed.Service.RecordRefundAsync(Request(seed, 150m));

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefundStatus.ShouldBe(BookingRefundStatus.Refunded);
        result.Value.RefundOpenAmount.ShouldBe(0m);
        var stored = await StoredRoomAsync(databaseName, seed.FirstId);
        stored.RefundPaidOn.ShouldBe(Today);
        stored.RefundPaidAmount.ShouldBe(150m);
        stored.RefundNote.ShouldBe("Stripe re_123");
    }

    [Fact]
    public async Task RecordAnotherAmountThanDecided()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);

        var result = await seed.Service.RecordRefundAsync(Request(seed, 200m));

        result.IsSuccess.ShouldBeTrue();
        var detail = await seed.Service.GetDetailAsync(seed.BookingId);
        detail.Value.RefundedAmount.ShouldBe(200m);
        detail.Value.RefundPaidAmount.ShouldBe(200m);
        detail.Value.RefundOpenAmount.ShouldBe(0m);
        detail.Value.Rooms.Single(x => x.BookingRoomId == seed.FirstId).Cancellation!.RefundAmount.ShouldBe(150m);
    }

    [Fact]
    public async Task ShowTheOpenRefundInTheDetail()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);

        var detail = await seed.Service.GetDetailAsync(seed.BookingId);

        detail.Value.Status.ShouldBe(BookingStatus.Paid);
        detail.Value.RefundStatus.ShouldBe(BookingRefundStatus.RefundPending);
        detail.Value.RefundOpenAmount.ShouldBe(150m);
        detail.Value.RefundPaidAmount.ShouldBe(0m);
        var cancellation = detail.Value.Rooms.Single(x => x.BookingRoomId == seed.FirstId).Cancellation!;
        cancellation.RefundPaidOn.ShouldBeNull();
        cancellation.RefundPaidAmount.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownBooking()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString());

        var result = await seed.Service.RecordRefundAsync(new BookingRequest.RecordRefund { BookingId = 999, BookingRoomId = seed.FirstId, PaidOn = Today, Amount = 10m });

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownRoom()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString());

        var result = await seed.Service.RecordRefundAsync(Request(seed, 10m, bookingRoomId: 999));

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task RefuseARefundForARoomThatIsNotCancelled()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);

        var result = await seed.Service.RecordRefundAsync(Request(seed, 10m, bookingRoomId: seed.SecondId));

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await StoredRoomAsync(databaseName, seed.SecondId)).IsRefundPaid.ShouldBeFalse();
    }

    [Fact]
    public async Task RefuseARefundWhenNoRefundWasDecided()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString(), cancelSecond: true);

        var result = await seed.Service.RecordRefundAsync(Request(seed, 10m, bookingRoomId: seed.SecondId));

        result.Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task RefuseARefundThatIsAlreadyPaid()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        (await seed.Service.RecordRefundAsync(Request(seed, 150m))).IsSuccess.ShouldBeTrue();

        var result = await seed.Service.RecordRefundAsync(Request(seed, 100m));

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await StoredRoomAsync(databaseName, seed.FirstId)).RefundPaidAmount.ShouldBe(150m);
    }

    [Fact]
    public async Task RefuseAPaymentDateInTheFuture()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString());

        var result = await seed.Service.RecordRefundAsync(Request(seed, 150m, paidOn: Today.AddDays(5)));

        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task RefuseMoreThanThePriceOfTheRoom()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString());

        var result = await seed.Service.RecordRefundAsync(Request(seed, 200.01m));

        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task RefuseMoreThanWasPaid()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName, refund: 100m, paid: 120m);

        var result = await seed.Service.RecordRefundAsync(Request(seed, 120.01m));

        result.Status.ShouldBe(ResultStatus.Invalid);
        (await StoredRoomAsync(databaseName, seed.FirstId)).IsRefundPaid.ShouldBeFalse();
    }

    [Fact]
    public async Task OpenTheRefundAgainWhenUndone()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        (await seed.Service.RecordRefundAsync(Request(seed, 150m))).IsSuccess.ShouldBeTrue();

        var result = await seed.Service.UndoRefundAsync(new BookingRequest.UndoRefund { BookingId = seed.BookingId, BookingRoomId = seed.FirstId });

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefundStatus.ShouldBe(BookingRefundStatus.RefundPending);
        result.Value.RefundOpenAmount.ShouldBe(150m);
        var stored = await StoredRoomAsync(databaseName, seed.FirstId);
        stored.IsRefundPaid.ShouldBeFalse();
        stored.RefundNote.ShouldBeNull();
    }

    [Fact]
    public async Task RefuseToUndoARefundThatIsNotPaid()
    {
        var seed = await SeedAsync(Guid.NewGuid().ToString());

        var result = await seed.Service.UndoRefundAsync(new BookingRequest.UndoRefund { BookingId = seed.BookingId, BookingRoomId = seed.FirstId });

        result.Status.ShouldBe(ResultStatus.Conflict);
    }
}
