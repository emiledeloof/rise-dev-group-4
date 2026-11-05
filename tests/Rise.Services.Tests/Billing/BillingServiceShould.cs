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
            x.Cancel(DateTime.UtcNow, "Guest is ill", 150m);
        });
        await using var _db = dbContext;

        var bill = (await new BillingService(dbContext).GetBillAsync(booking.Id)).Value;

        bill.Status.ShouldBe(BookingStatus.Cancelled);
        bill.OutstandingBalance.ShouldBe(0m);
        bill.Cancellation.ShouldNotBeNull();
        bill.Cancellation.RefundAmount.ShouldBe(150m);
        bill.Cancellation.Reason.ShouldBe("Guest is ill");
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
            x.Cancel(DateTime.UtcNow, "Guest is ill", 0m);
        });
        await using var _db = dbContext;
        var service = new BillingService(dbContext);
        var lineId = booking.BillLines.Single().Id;

        var added = await service.AddLineAsync(Minibar(booking.Id));
        var deleted = await service.DeleteLineAsync(new BillingRequest.DeleteLine { BookingId = booking.Id, LineId = lineId });

        added.Status.ShouldBe(ResultStatus.Conflict);
        deleted.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.BillLines.CountAsync(x => !x.IsDeleted)).ShouldBe(1);
    }
}
