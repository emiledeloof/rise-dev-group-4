using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
using Rise.Shared.Common;
using Rise.Shared.Guests;

namespace Rise.Services.Tests.Bookings;

/// <summary>
/// tests for cancelling rooms of a booking with different cancellation policies
/// the booking is for 3 guests and 2 nights, starting in the given nb of days:
/// the flexible room (fits 2, 100 a night, total 200, 100 pct from 30 days, 50 pct from 7 days)
/// the saver room (fits 1, 50 a night, total 100, non-refundable)
/// </summary>
public class BookingServiceCancelRoomsShould
{
    private const string Reason = "Guest asked to cancel by phone";

    private sealed record Seed(ApplicationDbContext DbContext, BookingService Service, int BookingId, int FlexibleId, int SaverId);

    private static async Task<Seed> SeedAsync(
        string databaseName,
        int daysUntilCheckIn = 60,
        decimal paid = 300m,
        IStripePaymentGateway? stripe = null,
        IConfiguration? configuration = null,
        IInterceptor? interceptor = null)
    {
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.GeneralSettings.Add(new GeneralSettings(false, 0m));

            var flexible = new Room("201", "Flexible Room", 2);
            var flexibleRate = flexible.AddRate("Flexible", 100m);
            var saver = new Room("202", "Saver Room", 1);
            var saverRate = saver.AddRate("Saver", 50m);
            saverRate.CancellationPolicy = CancellationPolicy.NonRefundable;

            var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));
            var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysUntilCheckIn);
            var booking = new Booking("20261001-SVM-000001", guest, checkIn, checkIn.AddDays(2), 3);
            booking.AddRoom(flexible, flexibleRate);
            booking.AddRoom(saver, saverRate);
            if (paid > 0)
                booking.AddPayment(paid).MarkAsPaid(DateTime.UtcNow.AddDays(-1));

            seedContext.Rooms.AddRange(flexible, saver);
            seedContext.Guests.Add(guest);
            seedContext.Bookings.Add(booking);
            await seedContext.SaveChangesAsync();
        }

        var dbContext = CreateContext(databaseName, interceptor);
        var rooms = await dbContext.BookingRooms.AsNoTracking().Include(x => x.Room).ToListAsync();
        var service = new BookingService(dbContext, stripe ?? Substitute.For<IStripePaymentGateway>(), configuration ?? Substitute.For<IConfiguration>());

        return new Seed(
            dbContext,
            service,
            (await dbContext.Bookings.AsNoTracking().SingleAsync()).Id,
            rooms.Single(x => x.Room.Name == "Flexible Room").Id,
            rooms.Single(x => x.Room.Name == "Saver Room").Id);
    }

    private static ApplicationDbContext CreateContext(string databaseName, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        return new ApplicationDbContext(builder.Options);
    }

    private static BookingRequest.Cancel Request(int bookingId, int? adults, params (int Id, decimal Refund)[] rooms) => new()
    {
        BookingId = bookingId,
        Reason = Reason,
        Adults = adults,
        Rooms = rooms.Select(x => new BookingRequest.Cancel.RoomSelection { BookingRoomId = x.Id, RefundAmount = x.Refund }).ToList(),
    };

    // what is really stored, read with a new context so nothing comes from the tracked entities of the service
    private static async Task<Booking> StoredAsync(string databaseName)
    {
        await using var dbContext = CreateContext(databaseName);
        return await dbContext.Bookings.AsNoTracking()
            .Include(x => x.Rooms).ThenInclude(x => x.Room)
            .SingleAsync();
    }

    private static async Task ShouldBeUntouchedAsync(string databaseName)
    {
        var stored = await StoredAsync(databaseName);
        stored.ActiveRooms.Count.ShouldBe(2);
        stored.RefundedAmount.ShouldBe(0m);
        stored.Adults.ShouldBe(3);
    }

    // ── policies ───────────────────────────────────────────────────────────────

    /// <summary>
    /// each room follows its own policy, the non-refundable one gives nothing however early
    /// </summary>
    [Theory]
    [InlineData(60, 100, 200)]
    [InlineData(30, 100, 200)]
    [InlineData(29, 50, 100)]
    [InlineData(7, 50, 100)]
    [InlineData(6, 0, 0)]
    public async Task ApplyTheOwnPolicyOfEachRoom(int daysUntilCheckIn, int flexiblePercent, decimal flexibleRefund)
    {
        // arrange
        var seed = await SeedAsync($"{nameof(ApplyTheOwnPolicyOfEachRoom)}-{daysUntilCheckIn}", daysUntilCheckIn);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.GetDetailAsync(seed.BookingId);

        // assert
        var flexible = result.Value.Rooms.Single(x => x.BookingRoomId == seed.FlexibleId);
        flexible.IsRefundable.ShouldBeTrue();
        flexible.RefundPercent.ShouldBe(flexiblePercent);
        flexible.RefundAmount.ShouldBe(flexibleRefund);
        var saver = result.Value.Rooms.Single(x => x.BookingRoomId == seed.SaverId);
        saver.IsRefundable.ShouldBeFalse();
        saver.RefundPercent.ShouldBe(0);
        saver.RefundAmount.ShouldBe(0m);
    }

    // ── partial and full cancellations ─────────────────────────────────────────

    [Fact]
    public async Task CancelOnlyTheSelectedRoom()
    {
        // arrange
        var databaseName = nameof(CancelOnlyTheSelectedRoom);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act, only the flexible room for 2 stays, so the 3 guests become 2
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.CancelledRoomCount.ShouldBe(1);
        result.Value.RemainingRoomCount.ShouldBe(1);
        result.Value.RefundAmount.ShouldBe(0m);
        var stored = await StoredAsync(databaseName);
        stored.IsPartiallyCancelled.ShouldBeTrue();
        stored.IsCancelled.ShouldBeFalse();
        stored.ActiveRooms.Single().Id.ShouldBe(seed.FlexibleId);
        stored.Rooms.Single(x => x.Id == seed.SaverId).CancellationReason.ShouldBe(Reason);
    }

    [Fact]
    public async Task RecordADifferentRefundPerRoomWhenCancellingTheWholeBooking()
    {
        // arrange
        var databaseName = nameof(RecordADifferentRefundPerRoomWhenCancellingTheWholeBooking);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 200m), (seed.SaverId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.RefundAmount.ShouldBe(200m);
        result.Value.RemainingRoomCount.ShouldBe(0);
        var stored = await StoredAsync(databaseName);
        stored.IsCancelled.ShouldBeTrue();
        stored.Rooms.Single(x => x.Id == seed.FlexibleId).RefundAmount.ShouldBe(200m);
        stored.Rooms.Single(x => x.Id == seed.SaverId).RefundAmount.ShouldBe(0m);
    }

    /// <summary>
    /// the admin may give goodwill on a non-refundable room, only the price of the room limits it
    /// </summary>
    [Fact]
    public async Task LetTheAdminOverruleThePolicyUpToThePriceOfTheRoom()
    {
        // arrange
        var databaseName = nameof(LetTheAdminOverruleThePolicyUpToThePriceOfTheRoom);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 100m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        (await StoredAsync(databaseName)).RefundedAmount.ShouldBe(100m);
    }

    /// <summary>
    /// the room cancelled earlier keeps the refund step of its own date, the later one gets the step of today
    /// </summary>
    [Fact]
    public async Task KeepTheDateAndRefundOfEachRoomOverSeveralCancellations()
    {
        // arrange, arrival in 10 days, the flexible room was already cancelled 40 days before arrival, so at 100 pct
        var databaseName = nameof(KeepTheDateAndRefundOfEachRoomOverSeveralCancellations);
        var seed = await SeedAsync(databaseName, daysUntilCheckIn: 10);
        await using var _ = seed.DbContext;
        var booking = await seed.DbContext.Bookings
            .Include(x => x.Rooms).ThenInclude(x => x.Room)
            .Include(x => x.Payments)
            .SingleAsync();
        var earlier = booking.CheckIn.AddDays(-40).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        booking.CancelRooms([new RoomCancellation(booking.Rooms.Single(x => x.Id == seed.FlexibleId), 200m)], earlier, "First reason", adults: 1);
        await seed.DbContext.SaveChangesAsync();

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.SaverId, 0m)));
        var detail = (await seed.Service.GetDetailAsync(seed.BookingId)).Value;

        // assert
        result.Value.RemainingRoomCount.ShouldBe(0);
        detail.Status.ShouldBe(BookingStatus.Cancelled);
        detail.RefundedAmount.ShouldBe(200m);
        detail.RefundableAmount.ShouldBe(100m);
        var flexible = detail.Rooms.Single(x => x.BookingRoomId == seed.FlexibleId);
        flexible.RefundReferenceDate.ShouldBe(DateOnly.FromDateTime(earlier));
        flexible.RefundPercent.ShouldBe(100);
        flexible.Cancellation!.Reason.ShouldBe("First reason");
        var saver = detail.Rooms.Single(x => x.BookingRoomId == seed.SaverId);
        saver.RefundReferenceDate.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
        saver.Cancellation!.Reason.ShouldBe(Reason);
    }

    [Fact]
    public async Task NotTouchStripeWhenCancellingARoom()
    {
        // arrange
        var stripe = Substitute.For<IStripePaymentGateway>();
        var seed = await SeedAsync(nameof(NotTouchStripeWhenCancellingARoom), stripe: stripe);
        await using var _ = seed.DbContext;

        // act
        await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 0m)));

        // assert
        stripe.ReceivedCalls().ShouldBeEmpty();
    }

    // ── refused cancellations ──────────────────────────────────────────────────

    [Fact]
    public async Task ReturnNotFoundForARoomOfAnotherBooking()
    {
        // arrange
        var databaseName = nameof(ReturnNotFoundForARoomOfAnotherBooking);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act, the valid room in the same request must not be cancelled
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.SaverId, 0m), (9999, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        await ShouldBeUntouchedAsync(databaseName);
    }

    [Fact]
    public async Task ReturnConflictForARoomThatIsAlreadyCancelled()
    {
        // arrange
        var databaseName = nameof(ReturnConflictForARoomThatIsAlreadyCancelled);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;
        (await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 10m)))).IsSuccess.ShouldBeTrue();

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 0m), (seed.SaverId, 50m)));

        // assert, the 1st cancellation is kept and the other room stays booked
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.Single().ShouldContain("202");
        var stored = await StoredAsync(databaseName);
        stored.Rooms.Single(x => x.Id == seed.SaverId).RefundAmount.ShouldBe(10m);
        stored.Rooms.Single(x => x.Id == seed.FlexibleId).IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnConflictWhenTheWholeBookingIsAlreadyCancelled()
    {
        // arrange
        var seed = await SeedAsync(nameof(ReturnConflictWhenTheWholeBookingIsAlreadyCancelled));
        await using var _ = seed.DbContext;
        await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 0m), (seed.SaverId, 0m)));

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.SaverId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.Single().ShouldContain("20261001-SVM-000001");
    }

    [Fact]
    public async Task RejectARefundAboveThePriceOfTheRoom()
    {
        // arrange, more than enough paid, the saver room costs 100
        var databaseName = nameof(RejectARefundAboveThePriceOfTheRoom);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 1m), (seed.SaverId, 100.01m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("100.00");
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("202");
        await ShouldBeUntouchedAsync(databaseName);
    }

    [Fact]
    public async Task RejectRefundsAboveTheDepositPaid()
    {
        // arrange, only 150 paid, each refund alone is fine but together they are too much
        var databaseName = nameof(RejectRefundsAboveTheDepositPaid);
        var seed = await SeedAsync(databaseName, paid: 150m);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 100m), (seed.SaverId, 50.01m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("150.00");
        await ShouldBeUntouchedAsync(databaseName);
    }

    [Fact]
    public async Task RejectRefundsAboveWhatIsLeftAfterAnEarlierCancellation()
    {
        // arrange, 150 paid and 100 refunded for the saver room, 50 is left
        var databaseName = nameof(RejectRefundsAboveWhatIsLeftAfterAnEarlierCancellation);
        var seed = await SeedAsync(databaseName, paid: 150m);
        await using var _ = seed.DbContext;
        (await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 100m)))).IsSuccess.ShouldBeTrue();

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 50.01m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("50.00");
        (await StoredAsync(databaseName)).IsCancelled.ShouldBeFalse();
    }

    // ── nb of guests ───────────────────────────────────────────────────────────

    /// <summary>
    /// 3 guests and only the flexible room for 2 stays, the admin has to enter the new nb
    /// </summary>
    [Fact]
    public async Task RejectACancellationThatLeavesTooManyGuests()
    {
        // arrange
        var databaseName = nameof(RejectACancellationThatLeavesTooManyGuests);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.SaverId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("at most 2 guests");
        await ShouldBeUntouchedAsync(databaseName);
    }

    [Fact]
    public async Task RejectANewGuestCountAboveTheRemainingCapacity()
    {
        // arrange, only the saver room for 1 stays
        var databaseName = nameof(RejectANewGuestCountAboveTheRemainingCapacity);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.FlexibleId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        await ShouldBeUntouchedAsync(databaseName);
    }

    [Fact]
    public async Task SaveTheNewGuestCount()
    {
        // arrange
        var databaseName = nameof(SaveTheNewGuestCount);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, adults: 1, (seed.FlexibleId, 0m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        (await StoredAsync(databaseName)).Adults.ShouldBe(1);
    }

    [Fact]
    public async Task NotAskForAGuestCountWhenCancellingTheWholeBooking()
    {
        // arrange
        var databaseName = nameof(NotAskForAGuestCountWhenCancellingTheWholeBooking);
        var seed = await SeedAsync(databaseName);
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 0m), (seed.SaverId, 0m)));

        // assert, the nb of the stay stays as history
        result.Status.ShouldBe(ResultStatus.Ok);
        (await StoredAsync(databaseName)).Adults.ShouldBe(3);
    }

    // ── concurrency ────────────────────────────────────────────────────────────

    /// <summary>
    /// another admin saves a cancellation of the same room between loading and saving, the 1st one must stay
    /// </summary>
    [Fact]
    public async Task ReturnConflictWhenTheRoomIsCancelledAtTheSameTime()
    {
        // arrange
        var databaseName = nameof(ReturnConflictWhenTheRoomIsCancelledAtTheSameTime);
        var seed = await SeedAsync(databaseName, interceptor: new CompetingCancellation(databaseName));
        await using var _ = seed.DbContext;

        // act
        var result = await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 20m)));

        // assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        var stored = await StoredAsync(databaseName);
        stored.Rooms.Single(x => x.Id == seed.SaverId).CancellationReason.ShouldBe("Other admin");
        stored.Rooms.Single(x => x.Id == seed.SaverId).RefundAmount.ShouldBe(5m);
    }

    /// <summary>
    /// cancels the saver room in a second context right before the service saves
    /// </summary>
    private sealed class CompetingCancellation(string databaseName) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await using var other = CreateContext(databaseName);
            var booking = await other.Bookings
                .Include(x => x.Rooms).ThenInclude(x => x.Room)
                .Include(x => x.Payments)
                .SingleAsync(cancellationToken);
            booking.CancelRooms(
                [new RoomCancellation(booking.Rooms.Single(x => x.Room.Name == "Saver Room"), 5m)],
                DateTime.UtcNow,
                "Other admin",
                adults: 2);
            await other.SaveChangesAsync(cancellationToken);

            return result;
        }
    }

    // ── availability ───────────────────────────────────────────────────────────

    private static BookingRequest.Create NewBooking(DateOnly checkIn, int roomId, int rateId) => new()
    {
        CheckIn = checkIn,
        CheckOut = checkIn.AddDays(2),
        Adults = 1,
        Guest = new GuestRequest.Create
        {
            FirstName = "Marie",
            LastName = "Peeters",
            Email = "marie@example.com",
            PhoneNumber = "+32 499 99 99 99",
            AddressLine1 = "Stationsstraat 5",
            Country = "Belgium",
        },
        Rooms = [new BookingRequest.Create.RoomSelection { RoomId = roomId, RateId = rateId }],
    };

    [Fact]
    public async Task MakeOnlyTheCancelledRoomBookableAgain()
    {
        // arrange
        var seed = await SeedAsync(nameof(MakeOnlyTheCancelledRoomBookableAgain));
        await using var _ = seed.DbContext;
        await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 0m)));
        var checkIn = (await seed.DbContext.Bookings.AsNoTracking().SingleAsync()).CheckIn;
        var rooms = await seed.DbContext.Rooms.AsNoTracking().Include(x => x.Rates).ToListAsync();
        var saver = rooms.Single(x => x.Name == "Saver Room");
        var flexible = rooms.Single(x => x.Name == "Flexible Room");

        // act
        var cancelledRoom = await seed.Service.CreateAsync(NewBooking(checkIn, saver.Id, saver.Rates.Single().Id));
        var keptRoom = await seed.Service.CreateAsync(NewBooking(checkIn, flexible.Id, flexible.Rates.Single().Id));

        // assert
        cancelledRoom.Status.ShouldBe(ResultStatus.Created);
        keptRoom.Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task HideOnlyTheCancelledRoomFromTheMatrix()
    {
        // arrange
        var seed = await SeedAsync(nameof(HideOnlyTheCancelledRoomFromTheMatrix));
        await using var _ = seed.DbContext;
        await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 0m)));
        var checkIn = (await seed.DbContext.Bookings.AsNoTracking().SingleAsync()).CheckIn;

        // act
        var result = await seed.Service.GetMatrixAsync(new BookingRequest.Matrix { WindowStart = checkIn, WindowDays = 7 });

        // assert
        result.Value.Rows.Single(x => x.RoomName == "Saver Room").Bookings.ShouldBeEmpty();
        result.Value.Rows.Single(x => x.RoomName == "Flexible Room").Bookings.ShouldHaveSingleItem();
    }

    // ── overviews ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task KeepThePaidStatusAndShowTheAmountsOfAPartlyCancelledBooking()
    {
        // arrange
        var seed = await SeedAsync(nameof(KeepThePaidStatusAndShowTheAmountsOfAPartlyCancelledBooking));
        await using var _ = seed.DbContext;
        await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 30m)));

        // act
        var detail = (await seed.Service.GetDetailAsync(seed.BookingId)).Value;
        var index = (await seed.Service.GetIndexAsync(new QueryRequest.SkipTake { Skip = 0, Take = 50 })).Value.Bookings.Single();

        // assert
        detail.Status.ShouldBe(BookingStatus.Paid);
        detail.TotalAmount.ShouldBe(300m);
        detail.ActiveAmount.ShouldBe(200m);
        detail.PaidAmount.ShouldBe(300m);
        detail.RefundedAmount.ShouldBe(30m);
        detail.RefundableAmount.ShouldBe(270m);
        detail.Adults.ShouldBe(2);
        detail.Rooms.Single(x => x.BookingRoomId == seed.FlexibleId).Cancellation.ShouldBeNull();
        detail.Rooms.Single(x => x.BookingRoomId == seed.SaverId).Cancellation!.RefundAmount.ShouldBe(30m);
        detail.Rooms.ShouldAllBe(x => x.MaxGuests > 0);

        index.Status.ShouldBe(BookingStatus.Paid);
        index.RoomCount.ShouldBe(2);
        index.CancelledRoomCount.ShouldBe(1);
        index.TotalAmount.ShouldBe(200m);
        index.RoomNumbers.ShouldBe(["201"]);
    }

    [Fact]
    public async Task KeepShowingWhatWasBookedOnTheListWhenAllRoomsAreCancelled()
    {
        // arrange
        var seed = await SeedAsync(nameof(KeepShowingWhatWasBookedOnTheListWhenAllRoomsAreCancelled));
        await using var _ = seed.DbContext;
        await seed.Service.CancelAsync(Request(seed.BookingId, null, (seed.FlexibleId, 0m), (seed.SaverId, 0m)));

        // act
        var index = (await seed.Service.GetIndexAsync(new QueryRequest.SkipTake { Skip = 0, Take = 50 })).Value.Bookings.Single();

        // assert
        index.Status.ShouldBe(BookingStatus.Cancelled);
        index.CancelledRoomCount.ShouldBe(2);
        index.TotalAmount.ShouldBe(300m);
        index.RoomNumbers.ShouldBe(["201", "202"], ignoreOrder: true);
    }

    /// <summary>
    /// the guest confirmation page only lists the rooms that are still booked
    /// </summary>
    [Fact]
    public async Task ListOnlyTheRemainingRoomsForTheGuest()
    {
        // arrange
        var seed = await SeedAsync(nameof(ListOnlyTheRemainingRoomsForTheGuest));
        await using var _ = seed.DbContext;
        (await seed.DbContext.Bookings.SingleAsync()).AddPayment(10m, "cs_guest");
        await seed.DbContext.SaveChangesAsync();
        await seed.Service.CancelAsync(Request(seed.BookingId, adults: 2, (seed.SaverId, 0m)));

        // act
        var result = await seed.Service.GetDetailByCheckoutSessionAsync("cs_guest");

        // assert
        result.Value.Rooms.ShouldHaveSingleItem().RoomNumber.ShouldBe("201");
    }
}
