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

        var accommodation = booking.Rooms
            .Where(x => !x.IsDeleted)
            .Select(x => new BillingDto.AccommodationLine
            {
                RoomNumber = x.Room.RoomNumber,
                RoomName = x.Room.Name,
                RateName = x.RateName,
                Nights = nights,
                PricePerNight = x.PricePerNight,
                VatPercentage = x.VatPercentage,
                Total = PriceCalculator.Total(x.PricePerNight, nights),
            })
            .OrderBy(x => x.RoomNumber)
            .ToList();

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
        var vatGroups = accommodation.Select(x => (x.VatPercentage, x.Total))
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
        var accommodationTotal = accommodation.Sum(x => x.Total);
        var otherServicesTotal = otherServices.Sum(x => x.Total);
        var total = accommodationTotal + otherServicesTotal;
        var vatAmount = vatGroups.Sum(x => x.VatAmount);
        var paidAmount = booking.PaidAmount;

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
            // a cancelled booking owes nothing, the refund decision is shown instead
            OutstandingBalance = booking.IsCancelled ? 0m : total - paidAmount,
            Cancellation = booking.IsCancelled
                ? new BillingDto.Cancellation
                {
                    CancelledAt = booking.Rooms.Where(x => !x.IsDeleted).Max(x => x.CancelledAt!.Value),
                    Reason = booking.Rooms.Where(x => !x.IsDeleted).MaxBy(x => x.CancelledAt)!.CancellationReason!,
                    RefundAmount = booking.RefundedAmount,
                }
                : null,
        };
    }
}
