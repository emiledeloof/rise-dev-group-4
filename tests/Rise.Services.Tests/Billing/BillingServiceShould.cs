using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Services.Billing;
using Rise.Shared.Billing;
using Rise.Shared.Bookings;

namespace Rise.Services.Tests.Billing;

/// <summary>
/// tests for the final bill, <see cref="BillingService"/>
/// an in memory db filled for each test, with the soft delete trigger so a deleted line stays in the db
/// </summary>
public class BillingServiceShould
{
    /// <summary>
    /// fresh db with 1 booking of 2 nights: room 101 at 110 per night (10 % VAT) and room 102 at 122 per night (22 % VAT)
    /// </summary>
    private static async Task<(ApplicationDbContext DbContext, Booking Booking)> CreateSeededDbContextAsync(
        string databaseName,
        Action<Booking>? tweak = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .UseTriggers(o => o.AddTrigger<EntityBeforeSaveTrigger>())
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var single = new Room("101", "Single Room", 1);
        var singleRate = single.AddRate("Standard", 100m);
        var twin = new Room("102", "Twin Room", 2);
        var twinRate = twin.AddRate("Flexible", 100m);

        var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-SVM-000001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
        booking.AddRoom(twin, twinRate, 122m, 22m);
        booking.AddRoom(single, singleRate, 110m, 10m);

        tweak?.Invoke(booking);

        dbContext.Rooms.AddRange(single, twin);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return (dbContext, booking);
    }

    private static BookingRoom Single(Booking booking) => booking.Rooms.Single(x => x.Room.RoomNumber == "101");

    private static BookingRoom Twin(Booking booking) => booking.Rooms.Single(x => x.Room.RoomNumber == "102");

    /// <summary>
    /// cancels room 102 of 244 for the stay, room 101 of 220 stays booked
    /// </summary>
    private static void CancelTwin(Booking booking, decimal refund)
        => booking.CancelRooms([new RoomCancellation(Twin(booking), refund)], DateTime.UtcNow, "Guest is ill", adults: 1);

    private static BillingRequest.AddLine Minibar(int bookingId) => new()
    {
        BookingId = bookingId,
        Description = "Minibar",
        Quantity = 2,
        UnitPrice = 8m,
        VatPercentage = 22m,
    };

    [Fact]
    public async Task ReturnNotFoundForUnknownBooking()
    {
        var (dbContext, _) = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForUnknownBooking));
        await using var _db = dbContext;

        var result = await new BillingService(dbContext).GetBillAsync(999);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ReturnNotFoundForDeletedBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForDeletedBooking), x => x.IsDeleted = true);
        await using var _db = dbContext;

        var result = await new BillingService(dbContext).GetBillAsync(booking.Id);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task SumTheAccommodationOfAllRooms()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(SumTheAccommodationOfAllRooms));
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Nights.ShouldBe(2);
        bill.Accommodation.Select(x => x.RoomNumber).ShouldBe(["101", "102"]);
        bill.Accommodation.Select(x => x.Total).ShouldBe([220m, 244m]);
        bill.AccommodationTotal.ShouldBe(464m);
        bill.OtherServicesTotal.ShouldBe(0m);
        bill.Total.ShouldBe(464m);
    }

    [Fact]
    public async Task GroupTheVatPerRate()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(GroupTheVatPerRate),
            x => x.AddBillLine("Minibar", 2, 8m, 22m));
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        // 10 %: 220 holds 20, 22 %: 244 + 16 = 260 holds 46.89
        bill.VatGroups.Select(x => x.VatPercentage).ShouldBe([10m, 22m]);
        bill.VatGroups[0].GrossAmount.ShouldBe(220m);
        bill.VatGroups[0].VatAmount.ShouldBe(20m);
        bill.VatGroups[1].GrossAmount.ShouldBe(260m);
        bill.VatGroups[1].VatAmount.ShouldBe(46.89m);
        bill.VatAmount.ShouldBe(66.89m);
        bill.NetAmount.ShouldBe(480m - 66.89m);
        bill.Total.ShouldBe(480m);
    }

    [Fact]
    public async Task IncludeOtherServicesInTheTotal()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(IncludeOtherServicesInTheTotal), x =>
        {
            x.AddBillLine("Minibar", 2, 8m, 22m);
            x.AddBillLine("Guided vineyard tour", 1, 45m, 22m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.OtherServices.Select(x => x.Description).ShouldBe(["Minibar", "Guided vineyard tour"]);
        bill.OtherServicesTotal.ShouldBe(61m);
        bill.Total.ShouldBe(525m);
        bill.OutstandingBalance.ShouldBe(525m);
    }

    [Fact]
    public async Task IgnoreDeletedOtherServices()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(IgnoreDeletedOtherServices),
            x => x.AddBillLine("Minibar", 2, 8m, 22m).IsDeleted = true);
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.OtherServices.ShouldBeEmpty();
        bill.Total.ShouldBe(464m);
    }

    [Fact]
    public async Task OnlySubtractSettledPayments()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(OnlySubtractSettledPayments), x =>
        {
            x.AddPayment(300m).MarkAsPaid(DateTime.UtcNow);
            x.AddPayment(164m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Payments.Count.ShouldBe(2);
        bill.PaidAmount.ShouldBe(300m);
        bill.OutstandingBalance.ShouldBe(164m);
        bill.Status.ShouldBe(BookingStatus.Paid);
    }

    [Fact]
    public async Task IgnoreDeletedPayments()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(IgnoreDeletedPayments), x =>
        {
            var payment = x.AddPayment(464m);
            payment.MarkAsPaid(DateTime.UtcNow);
            payment.IsDeleted = true;
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Payments.ShouldBeEmpty();
        bill.PaidAmount.ShouldBe(0m);
        bill.OutstandingBalance.ShouldBe(464m);
        bill.Status.ShouldBe(BookingStatus.Pending);
    }

    [Fact]
    public async Task ShowACreditWhenOverpaid()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ShowACreditWhenOverpaid),
            x => x.AddPayment(500m).MarkAsPaid(DateTime.UtcNow));
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.OutstandingBalance.ShouldBe(-36m);
    }

    [Fact]
    public async Task ShowNothingDueForACancelledBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ShowNothingDueForACancelledBooking), x =>
        {
            x.AddPayment(200m).MarkAsPaid(DateTime.UtcNow);
            x.CancelRooms([new RoomCancellation(x.Rooms[0], 150m), new RoomCancellation(x.Rooms[1], 0m)], DateTime.UtcNow, "Guest is ill");
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Status.ShouldBe(BookingStatus.Cancelled);
        bill.IsCancelled.ShouldBeTrue();
        bill.OutstandingBalance.ShouldBe(0m);
        bill.RefundedAmount.ShouldBe(150m);
        bill.Accommodation.ShouldAllBe(x => x.Cancellation != null && x.Cancellation.Reason == "Guest is ill");
        bill.Accommodation.Sum(x => x.Cancellation!.RefundAmount).ShouldBe(150m);
    }

    [Fact]
    public async Task KeepWhatIsPaidAndNotRefundedAsFeesOfAFullyCancelledBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(KeepWhatIsPaidAndNotRefundedAsFeesOfAFullyCancelledBooking), x =>
        {
            x.AddPayment(464m).MarkAsPaid(DateTime.UtcNow);
            x.CancelRooms([new RoomCancellation(Twin(x), 100m), new RoomCancellation(Single(x), 0m)], DateTime.UtcNow, "Guest is ill");
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.AccommodationTotal.ShouldBe(0m);
        bill.CancellationFeesTotal.ShouldBe(364m);
        bill.Total.ShouldBe(364m);
        bill.OutstandingBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task BillTheRemainingRoomAndTheFeeOfAPartlyCancelledBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(BillTheRemainingRoomAndTheFeeOfAPartlyCancelledBooking), x =>
        {
            x.AddPayment(464m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 100m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Status.ShouldBe(BookingStatus.Paid);
        bill.IsCancelled.ShouldBeFalse();
        bill.IsPartiallyCancelled.ShouldBeTrue();
        bill.AccommodationTotal.ShouldBe(220m);
        bill.CancellationFeesTotal.ShouldBe(144m);
        bill.Total.ShouldBe(364m);
        bill.PaidAmount.ShouldBe(464m);
        bill.RefundedAmount.ShouldBe(100m);
        bill.OutstandingBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task KeepTheCancelledRoomOnTheBillForTheRecords()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(KeepTheCancelledRoomOnTheBillForTheRecords), x =>
        {
            x.AddPayment(464m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 100m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Accommodation.Select(x => x.RoomNumber).ShouldBe(["101", "102"]);
        bill.Accommodation[0].Cancellation.ShouldBeNull();
        bill.Accommodation[0].CancellationFee.ShouldBe(0m);
        bill.Accommodation[1].Total.ShouldBe(244m);
        bill.Accommodation[1].CancellationFee.ShouldBe(144m);
        var cancellation = bill.Accommodation[1].Cancellation.ShouldNotBeNull();
        cancellation.Reason.ShouldBe("Guest is ill");
        cancellation.RefundAmount.ShouldBe(100m);
    }

    [Fact]
    public async Task NotLetAFreeLineLowerTheCancellationFee()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(NotLetAFreeLineLowerTheCancellationFee), x =>
        {
            x.AddPayment(464m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 100m);
            x.AddBillLine("Minibar", 2, 8m, 22m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.CancellationFeesTotal.ShouldBe(144m);
        bill.Total.ShouldBe(380m);
        bill.OutstandingBalance.ShouldBe(16m);
    }

    [Fact]
    public async Task ChargeNoFeeForACancelledRoomWhenNothingIsPaid()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ChargeNoFeeForACancelledRoomWhenNothingIsPaid), x => CancelTwin(x, 0m));
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Status.ShouldBe(BookingStatus.Pending);
        bill.CancellationFeesTotal.ShouldBe(0m);
        bill.Total.ShouldBe(220m);
        bill.OutstandingBalance.ShouldBe(220m);
    }

    [Fact]
    public async Task ChargeNoFeeWhenTheDepositOnlyCoversTheRemainingRoom()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ChargeNoFeeWhenTheDepositOnlyCoversTheRemainingRoom), x =>
        {
            x.AddPayment(220m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 0m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.CancellationFeesTotal.ShouldBe(0m);
        bill.Total.ShouldBe(220m);
        bill.OutstandingBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task LimitTheFeeToWhatWasPaidAboveTheRemainingRoom()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(LimitTheFeeToWhatWasPaidAboveTheRemainingRoom), x =>
        {
            x.AddPayment(300m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 0m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.CancellationFeesTotal.ShouldBe(80m);
        bill.Total.ShouldBe(300m);
        bill.OutstandingBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task OnlyCountTheVatOfTheFeeForACancelledRoom()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(OnlyCountTheVatOfTheFeeForACancelledRoom), x =>
        {
            x.AddPayment(464m).MarkAsPaid(DateTime.UtcNow);
            CancelTwin(x, 100m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        // 10 %: 220 holds 20, 22 %: the fee of 144 holds 25.97
        bill.VatGroups.Select(x => x.VatPercentage).ShouldBe([10m, 22m]);
        bill.VatGroups[1].GrossAmount.ShouldBe(144m);
        bill.VatGroups[1].VatAmount.ShouldBe(25.97m);
        bill.VatAmount.ShouldBe(45.97m);
        bill.NetAmount.ShouldBe(364m - 45.97m);
    }

    [Fact]
    public async Task NotShowAVatGroupForACancelledRoomWithoutFee()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(NotShowAVatGroupForACancelledRoomWithoutFee), x => CancelTwin(x, 0m));
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.VatGroups.ShouldHaveSingleItem().VatPercentage.ShouldBe(10m);
    }

    [Fact]
    public async Task LetTheAdminChangeTheBillOfAPartlyCancelledBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(LetTheAdminChangeTheBillOfAPartlyCancelledBooking), x => CancelTwin(x, 0m));
        await using var _db = dbContext;
        var service = new BillingService(dbContext);
        var bookingId = booking.Id;
        // the service must load the rooms itself, tracked rooms would hide a missing include
        dbContext.ChangeTracker.Clear();

        var added = await service.AddLineAsync(Minibar(bookingId));
        added.IsSuccess.ShouldBeTrue();
        added.Value.Total.ShouldBe(236m);
        dbContext.ChangeTracker.Clear();

        var lineId = added.Value.OtherServices.Single().Id;
        var edited = await service.EditLineAsync(new BillingRequest.EditLine
        {
            BookingId = bookingId,
            LineId = lineId,
            Description = "Wine tasting",
            Quantity = 1,
            UnitPrice = 30m,
            VatPercentage = 10m,
        });
        edited.IsSuccess.ShouldBeTrue();
        edited.Value.Total.ShouldBe(250m);
        dbContext.ChangeTracker.Clear();

        var deleted = await service.DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = bookingId, LineId = lineId });
        deleted.IsSuccess.ShouldBeTrue();
        deleted.Value.Total.ShouldBe(220m);
    }

    [Fact]
    public async Task AddALine()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(AddALine));
        await using var _db = dbContext;

        var result = await new BillingService(dbContext).AddLineAsync(Minibar(booking.Id));

        result.IsSuccess.ShouldBeTrue();
        var line = result.Value.OtherServices.ShouldHaveSingleItem();
        line.Description.ShouldBe("Minibar");
        line.Total.ShouldBe(16m);
        result.Value.Total.ShouldBe(480m);
        (await dbContext.BillLines.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task EditALine()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(EditALine),
            x => x.AddBillLine("Minibar", 2, 8m, 22m));
        await using var _db = dbContext;
        var lineId = booking.BillLines.Single().Id;

        var result = await new BillingService(dbContext).EditLineAsync(new BillingRequest.EditLine
        {
            BookingId = booking.Id,
            LineId = lineId,
            Description = "Wine tasting",
            Quantity = 1,
            UnitPrice = 30m,
            VatPercentage = 10m,
        });

        result.IsSuccess.ShouldBeTrue();
        var line = result.Value.OtherServices.ShouldHaveSingleItem();
        line.Description.ShouldBe("Wine tasting");
        line.Total.ShouldBe(30m);
        line.VatPercentage.ShouldBe(10m);
    }

    [Fact]
    public async Task DeleteALineSoftly()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(DeleteALineSoftly),
            x => x.AddBillLine("Minibar", 2, 8m, 22m));
        await using var _db = dbContext;
        var lineId = booking.BillLines.Single().Id;

        var result = await new BillingService(dbContext).DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = booking.Id, LineId = lineId });

        result.IsSuccess.ShouldBeTrue();
        result.Value.OtherServices.ShouldBeEmpty();
        result.Value.Total.ShouldBe(464m);
        // still in the db for the records, only marked as deleted
        (await dbContext.BillLines.SingleAsync(x => x.Id == lineId)).IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnNotFoundForALineOfAnotherBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForALineOfAnotherBooking),
            x => x.AddBillLine("Minibar", 2, 8m, 22m));
        await using var _db = dbContext;
        var lineId = booking.BillLines.Single().Id;

        var result = await new BillingService(dbContext).DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = booking.Id + 1, LineId = lineId });

        result.Status.ShouldBe(ResultStatus.NotFound);
        (await dbContext.BillLines.SingleAsync()).IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnConflictWhenChangingTheBillOfACancelledBooking()
    {
        var (dbContext, booking) = await CreateSeededDbContextAsync(nameof(ReturnConflictWhenChangingTheBillOfACancelledBooking), x =>
        {
            x.AddBillLine("Minibar", 2, 8m, 22m);
            x.CancelRooms([new RoomCancellation(x.Rooms[0], 0m), new RoomCancellation(x.Rooms[1], 0m)], DateTime.UtcNow, "Guest is ill");
        });
        await using var _db = dbContext;
        var service = new BillingService(dbContext);
        var bookingId = booking.Id;
        var lineId = booking.BillLines.Single().Id;
        // the service must load the rooms itself, tracked rooms would hide a missing include
        dbContext.ChangeTracker.Clear();

        var added = await service.AddLineAsync(Minibar(bookingId));
        var deleted = await service.DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = bookingId, LineId = lineId });

        added.Status.ShouldBe(ResultStatus.Conflict);
        deleted.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.BillLines.CountAsync(x => !x.IsDeleted)).ShouldBe(1);
    }
}
