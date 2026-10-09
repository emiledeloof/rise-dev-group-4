using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Common;
using Rise.Shared.Invoice;

namespace Rise.Services.Invoice;

public class InvoiceService(ApplicationDbContext dbContext) : IInvoiceService
{
    public async Task<Result<InvoiceResponse.Index>> GetIndexAsync(QueryRequest.SkipTake request, CancellationToken ctx = default)
    {
        var query = dbContext.Payments.Where(p => !p.IsDeleted);

        var totalCount = await query.CountAsync(ctx);

        if (!string.IsNullOrWhiteSpace(request.OrderBy))
        {
            query = request.OrderDescending
                ? query.OrderByDescending(e => EF.Property<object>(e, request.OrderBy))
                : query.OrderBy(e => EF.Property<object>(e, request.OrderBy));
        }
        else
        {
            // Newest payments first, so paging is deterministic.
            query = query.OrderByDescending(p => p.Id);
        }

        var invoices = await query.AsNoTracking()
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new InvoiceDto.Index
            {
                Id = p.Id,
                BookingNumber = p.Booking.BookingNumber,
                GuestName = p.Booking.Guest.Name,
                Amount = p.Amount,
                PaidAt = p.PaidAt,
                InvoiceNumber = p.InvoiceNumber,
                StripeInvoice = p.StripeInvoice,
                StripePaymentIntent = p.StripePaymentIntent,
                StripeReceiptUrl = p.StripeReceiptUrl,
                CardBrand = p.CardBrand,
                CardLast4 = p.CardLast4
            })
            .ToListAsync(ctx);

        return Result.Success(new InvoiceResponse.Index
        {
            Invoices = invoices,
            TotalCount = totalCount
        });
    }
}
