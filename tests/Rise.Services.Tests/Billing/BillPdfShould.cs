using Rise.Services.Billing;
using Rise.Shared.Billing;
using Rise.Shared.Bookings;
using Shouldly;

namespace Rise.Services.Tests.Billing;

public class BillPdfShould
{
    private static BillingDto.Bill Bill() => new()
    {
        BookingId = 7,
        BookingNumber = "20261004-SVM-000007",
        Status = BookingStatus.Settled,
        GuestName = "Jan Janssens",
        CheckIn = new DateOnly(2026, 10, 10),
        CheckOut = new DateOnly(2026, 10, 12),
        Nights = 2,
        Accommodation =
        [
            new() { RoomNumber = "101", RoomName = "Standard Room", RateName = "Flexible", Nights = 2, PricePerNight = 100m, VatPercentage = 10m, Total = 200m },
        ],
        OtherServices =
        [
            new() { Id = 1, Description = "Minibar", Quantity = 1, UnitPrice = 8m, VatPercentage = 22m, Total = 8m },
        ],
        AccommodationTotal = 200m,
        OtherServicesTotal = 8m,
        VatGroups = [],
        NetAmount = 188.36m,
        VatAmount = 19.64m,
        Total = 208m,
        Payments = [],
        PaidAmount = 50m,
        RefundedAmount = 0m,
        CancellationFeesTotal = 0m,
        OutstandingBalance = 158m,
        SettledAt = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void CreateAPdfFile()
    {
        var bytes = BillPdf.Create(Bill());

        // every pdf file starts with %PDF
        bytes.Take(4).ShouldBe("%PDF"u8.ToArray());
    }

    [Fact]
    public void CreateAPdfForAnOpenBillWithoutOtherServices()
    {
        var bill = Bill();
        bill.SettledAt = null;
        bill.OtherServices = [];

        BillPdf.Create(bill).Length.ShouldBeGreaterThan(0);
    }
}
