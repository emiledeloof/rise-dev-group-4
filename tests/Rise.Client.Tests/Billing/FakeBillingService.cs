using Ardalis.Result;
using Rise.Shared.Billing;
using Rise.Shared.Bookings;

namespace Rise.Client.Billing;

/// <summary>
/// fake IBillingService for the bill page tests, no http call
/// each test chooses what the calls send back, a bill with 1 room and 1 minibar line by default
/// </summary>
public class FakeBillingService : IBillingService
{
    public Result<BillingDto.Bill> BillAnswer { get; set; } = Result.Success(TestBills.Open());

    /// <summary>
    /// what add, edit and delete send back, the bill with the new totals by default
    /// </summary>
    public Result<BillingDto.Bill> ChangeAnswer { get; set; } = Result.Success(TestBills.Open(otherServicesTotal: 61m));

    public List<BillingRequest.AddLine> AddRequests { get; } = [];
    public List<BillingRequest.EditLine> EditRequests { get; } = [];
    public List<BillingRequest.DeleteLine> DeleteRequests { get; } = [];
    public List<BillingRequest.Settle> SettleRequests { get; } = [];

    public Task<Result<BillingDto.Bill>> GetBillAsync(int bookingId, CancellationToken ctx = default)
        => Task.FromResult(BillAnswer);

    public Task<Result<BillingDto.Bill>> AddLineAsync(BillingRequest.AddLine request, CancellationToken ctx = default)
    {
        AddRequests.Add(request);
        return Task.FromResult(ChangeAnswer);
    }

    public Task<Result<BillingDto.Bill>> EditLineAsync(BillingRequest.EditLine request, CancellationToken ctx = default)
    {
        EditRequests.Add(request);
        return Task.FromResult(ChangeAnswer);
    }

    public Task<Result<BillingDto.Bill>> DeleteLineAsync(BillingRequest.DeleteLine request, CancellationToken ctx = default)
    {
        DeleteRequests.Add(request);
        return Task.FromResult(ChangeAnswer);
    }

    public Task<Result<BillingDto.Bill>> SettleAsync(BillingRequest.Settle request, CancellationToken ctx = default)
    {
        SettleRequests.Add(request);
        return Task.FromResult(ChangeAnswer);
    }
}

/// <summary>
/// bills for the page tests
/// </summary>
public static class TestBills
{
    /// <summary>
    /// 3 nights in room 101 at €110 (10 % VAT) plus minibar, €200 paid and an open Stripe session
    /// </summary>
    public static BillingDto.Bill Open(decimal otherServicesTotal = 16m) => new()
    {
        BookingId = 7,
        BookingNumber = "20261004-SVM-000007",
        Status = "Paid",
        GuestName = "Lukas Müller",
        CheckIn = new DateOnly(2026, 10, 17),
        CheckOut = new DateOnly(2026, 10, 20),
        Nights = 3,
        Accommodation =
        [
            new BillingDto.AccommodationLine
            {
                RoomNumber = "101",
                RoomName = "Standard Room",
                RateName = "Flexible",
                Nights = 3,
                PricePerNight = 110m,
                VatPercentage = 10m,
                Total = 330m,
            },
        ],
        OtherServices =
        [
            new BillingDto.OtherServiceLine
            {
                Id = 11,
                Description = "Minibar",
                Quantity = 2,
                UnitPrice = 8m,
                VatPercentage = 22m,
                Total = 16m,
            },
        ],
        AccommodationTotal = 330m,
        CancellationFeesTotal = 0m,
        OtherServicesTotal = otherServicesTotal,
        VatGroups =
        [
            new BillingDto.VatGroup { VatPercentage = 10m, GrossAmount = 330m, NetAmount = 300m, VatAmount = 30m },
            new BillingDto.VatGroup { VatPercentage = 22m, GrossAmount = 16m, NetAmount = 13.11m, VatAmount = 2.89m },
        ],
        NetAmount = 313.11m,
        VatAmount = 32.89m,
        Total = 330m + otherServicesTotal,
        Payments =
        [
            new BillingDto.Payment { Id = 1, Amount = 200m, PaidAt = new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc), InvoiceNumber = "INV-2026-0002" },
            new BillingDto.Payment { Id = 2, Amount = 130m },
        ],
        PaidAmount = 200m,
        RefundedAmount = 0m,
        OutstandingBalance = 130m + otherServicesTotal,
    };

    /// <summary>the open bill once settled, settling records no payment so the balance stays open</summary>
    public static BillingDto.Bill Settled()
    {
        var bill = Open();
        bill.Status = BookingStatus.Settled;
        bill.SettledAt = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        return bill;
    }

    public static BillingDto.Bill Cancelled()
    {
        var bill = Open();
        bill.Status = "Cancelled";
        bill.OutstandingBalance = 0m;
        bill.RefundedAmount = 150m;
        bill.Accommodation[0].Cancellation = new BillingDto.Cancellation
        {
            CancelledAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            Reason = "Guest is ill",
            RefundAmount = 150m,
        };
        return bill;
    }

    /// <summary>
    /// <see cref="Open"/> with a 2nd room, room 102 cancelled with a €100 refund and a €50 fee
    /// </summary>
    public static BillingDto.Bill PartlyCancelled()
    {
        var bill = Open();
        bill.Accommodation.Add(new BillingDto.AccommodationLine
        {
            RoomNumber = "102",
            RoomName = "Deluxe Room",
            RateName = "Flexible",
            Nights = 3,
            PricePerNight = 50m,
            VatPercentage = 10m,
            Total = 150m,
            CancellationFee = 50m,
            Cancellation = new BillingDto.Cancellation
            {
                CancelledAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
                Reason = "Guest is ill",
                RefundAmount = 100m,
            },
        });
        bill.CancellationFeesTotal = 50m;
        bill.RefundedAmount = 100m;
        bill.Total += 50m;
        bill.OutstandingBalance = 0m;
        return bill;
    }
}
