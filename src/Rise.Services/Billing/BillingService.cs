using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Services.Pricing;
using Rise.Shared.Billing;

namespace Rise.Services.Billing;

/// <summary>
/// The final bill of a booking: the rooms and free lines the guest used, the payments and the open balance.
/// Nothing is stored on the bill itself, it is worked out from the booking every time,
/// the VAT comes from the pct saved on each line.
/// </summary>
public class BillingService(ApplicationDbContext dbContext) : IBillingService
{
    /// <summary>
    /// A deleted booking is treated as not found, a cancelled one still has its bill.
    /// </summary>
    public async Task<Result<BillingDto.Bill>> GetBillAsync(int bookingId, CancellationToken ctx = default)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Guest)
            .Include(x => x.Rooms)
                .ThenInclude(x => x.Room)
            .Include(x => x.Payments)
            .Include(x => x.BillLines)
            .SingleOrDefaultAsync(x => x.Id == bookingId && !x.IsDeleted, ctx);

        if (booking is null)
        {
            return Result.NotFound($"Booking with ID '{bookingId}' was not found.");
        }

        return Result.Success(ToBill(booking));
    }

    public async Task<Result<BillingDto.Bill>> AddLineAsync(BillingRequest.AddLine request, CancellationToken ctx = default)
    {
        // ! the rooms are needed, IsCancelled is derived from them
        var booking = await dbContext.Bookings
            .Include(x => x.Rooms)
            .SingleOrDefaultAsync(x => x.Id == request.BookingId && !x.IsDeleted, ctx);

        if (booking is null)
        {
            return Result.NotFound($"Booking with ID '{request.BookingId}' was not found.");
        }

        if (booking.IsCancelled)
        {
            return CancelledConflict(booking);
        }

        booking.AddBillLine(request.Description, request.Quantity, request.UnitPrice, request.VatPercentage);
        await dbContext.SaveChangesAsync(ctx);

        return await GetBillAsync(booking.Id, ctx);
    }

    public async Task<Result<BillingDto.Bill>> EditLineAsync(BillingRequest.EditLine request, CancellationToken ctx = default)
    {
        var line = await FindLineAsync(request.BookingId, request.LineId, ctx);

        if (line is null)
        {
            return Result.NotFound($"Line with ID '{request.LineId}' was not found on this bill.");
        }

        if (line.Booking.IsCancelled)
        {
            return CancelledConflict(line.Booking);
        }

        line.Update(request.Description, request.Quantity, request.UnitPrice, request.VatPercentage);
        await dbContext.SaveChangesAsync(ctx);

        return await GetBillAsync(request.BookingId, ctx);
    }

    /// <summary>
    /// Soft deletes the line, the EntityBeforeSaveTrigger turns the remove into IsDeleted.
    /// </summary>
    public async Task<Result<BillingDto.Bill>> DeleteLineAsync(BillingRequest.DeleteLine request, CancellationToken ctx = default)
    {
        var line = await FindLineAsync(request.BookingId, request.LineId, ctx);

        if (line is null)
        {
            return Result.NotFound($"Line with ID '{request.LineId}' was not found on this bill.");
        }

        if (line.Booking.IsCancelled)
        {
            return CancelledConflict(line.Booking);
        }

        dbContext.BillLines.Remove(line);
        await dbContext.SaveChangesAsync(ctx);

        return await GetBillAsync(request.BookingId, ctx);
    }

    /// <summary>
    /// The line only counts when it belongs to the booking from the route, a line of another booking is not found.
    /// ! the rooms are needed, IsCancelled is derived from them
    /// </summary>
    private Task<BillLine?> FindLineAsync(int bookingId, int lineId, CancellationToken ctx)
        => dbContext.BillLines
            .Include(x => x.Booking)
                .ThenInclude(x => x.Rooms)
            .SingleOrDefaultAsync(x => x.Id == lineId
                && !x.IsDeleted
                && x.Booking.Id == bookingId
                && !x.Booking.IsDeleted, ctx);

    private static Result<BillingDto.Bill> CancelledConflict(Booking booking)
        => Result.Conflict($"Booking {booking.BookingNumber} is cancelled, its bill can't be changed.");

    private static BillingDto.Bill ToBill(Booking booking)
    {
        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;

        var accommodation = ToAccommodationLines(booking, nights);

        var otherServices = booking.BillLines
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .Select(x => new BillingDto.OtherServiceLine
            {
                Id = x.Id,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                VatPercentage = x.VatPercentage,
                Total = x.Total,
            })
            .ToList();

        // VAT per pct on the sum of its lines, so rounding happens once per group and not once per line
        // a cancelled room only carries VAT on its fee, one without a fee adds nothing
        var vatGroups = accommodation.Where(x => x.ChargedAmount > 0).Select(x => (x.VatPercentage, Total: x.ChargedAmount))
            .Concat(otherServices.Select(x => (x.VatPercentage, x.Total)))
            .GroupBy(x => x.VatPercentage)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var gross = group.Sum(x => x.Total);
                var vat = PriceCalculator.VatIncluded(gross, group.Key);
                return new BillingDto.VatGroup
                {
                    VatPercentage = group.Key,
                    GrossAmount = gross,
                    NetAmount = gross - vat,
                    VatAmount = vat,
                };
            })
            .ToList();

        var payments = booking.Payments.Where(x => !x.IsDeleted).ToList();
        var accommodationTotal = accommodation.Where(x => !x.IsCancelled).Sum(x => x.Total);
        var cancellationFeesTotal = accommodation.Sum(x => x.CancellationFee);
        var otherServicesTotal = otherServices.Sum(x => x.Total);
        var total = accommodationTotal + cancellationFeesTotal + otherServicesTotal;
        var vatAmount = vatGroups.Sum(x => x.VatAmount);
        var paidAmount = booking.PaidAmount;
        var refundedAmount = booking.RefundedAmount;

        return new BillingDto.Bill
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            Status = BookingStatusRule.For(booking),
            GuestName = booking.Guest.Name,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Nights = nights,
            Accommodation = accommodation,
            OtherServices = otherServices,
            AccommodationTotal = accommodationTotal,
            CancellationFeesTotal = cancellationFeesTotal,
            OtherServicesTotal = otherServicesTotal,
            VatGroups = vatGroups,
            NetAmount = total - vatAmount,
            VatAmount = vatAmount,
            Total = total,
            Payments = payments
                .OrderBy(x => x.Id)
                .Select(x => new BillingDto.Payment
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    PaidAt = x.PaidAt,
                    InvoiceNumber = x.InvoiceNumber,
                    CardBrand = x.CardBrand,
                    CardLast4 = x.CardLast4,
                    StripeReceiptUrl = x.StripeReceiptUrl,
                })
                .ToList(),
            PaidAmount = paidAmount,
            RefundedAmount = refundedAmount,
            // a fully cancelled booking owes nothing, the refund decision is shown instead
            OutstandingBalance = booking.IsCancelled ? 0m : total - (paidAmount - refundedAmount),
        };
    }

    /// <summary>
    /// The rooms with what each one costs. The fee of a cancelled room is what the hotel keeps of it:
    /// its price minus the refund, but never more than what was paid on top of the rooms still booked.
    /// So an unpaid room or a deposit that goes to the remaining rooms gives no fee, like checkout doesn't charge cancelled rooms.
    /// The free lines stay out of this, adding a minibar line must not lower the fee.
    /// </summary>
    private static List<BillingDto.AccommodationLine> ToAccommodationLines(Booking booking, int nights)
    {
        var rooms = booking.Rooms.Where(x => !x.IsDeleted).OrderBy(x => x.Room.RoomNumber).ToList();
        var activeTotal = rooms.Where(x => !x.IsCancelled).Sum(x => PriceCalculator.Total(x.PricePerNight, nights));
        var paidAboveActiveRooms = Math.Max(booking.PaidAmount - booking.RefundedAmount - activeTotal, 0m);

        var lines = new List<BillingDto.AccommodationLine>();
        foreach (var room in rooms)
        {
            var total = PriceCalculator.Total(room.PricePerNight, nights);
            // a refund paid back with another amount counts with what was really paid
            var refund = room.EffectiveRefund;
            var fee = room.IsCancelled ? Math.Min(total - refund, paidAboveActiveRooms) : 0m;
            paidAboveActiveRooms -= fee;

            lines.Add(new BillingDto.AccommodationLine
            {
                RoomNumber = room.Room.RoomNumber,
                RoomName = room.Room.Name,
                RateName = room.RateName,
                Nights = nights,
                PricePerNight = room.PricePerNight,
                VatPercentage = room.VatPercentage,
                Total = total,
                CancellationFee = fee,
                Cancellation = room.CancelledAt is { } cancelledAt
                    ? new BillingDto.Cancellation
                    {
                        CancelledAt = cancelledAt,
                        Reason = room.CancellationReason!,
                        RefundAmount = refund,
                    }
                    : null,
            });
        }

        return lines;
    }
}
