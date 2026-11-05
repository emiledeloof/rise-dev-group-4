using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.Result;
using Rise.Shared.Common;
using Rise.Shared.Invoice;

namespace Rise.Client.Invoice;

public class FakeInvoiceService : IInvoiceService
{
    public Task<Result<InvoiceResponse.Index>> GetIndexAsync(QueryRequest.SkipTake request, CancellationToken ctx = default)
    {
        var invoices = Enumerable.Range(1, 3)
            .Select(i => new InvoiceDto.Index
            {
                Id = i,
                BookingId = i,
                BookingNumber = $"20261001-000{i}",
                GuestName = $"Guest {i}",
                Amount = i * 100m,
                InvoiceNumber = i == 1 ? "INV-2026-0001" : null,
                PaidAt = i == 1 ? new DateTime(2026, 10, 1) : null,
                StripePaymentIntent = i == 1 ? "pi_test_1" : null,
                StripeReceiptUrl = i == 1 ? "https://pay.stripe.com/receipts/test_1" : null,
                CardBrand = i == 1 ? "visa" : null,
                CardLast4 = i == 1 ? "4242" : null,
                // 1st row nothing cancelled, 2nd partly with a refund, 3rd fully without a refund
                RoomCount = 2,
                CancelledRoomCount = i - 1,
                RefundedAmount = i == 2 ? 150m : 0m,
            });

        var wrapper = new InvoiceResponse.Index
        {
            Invoices = invoices,
            TotalCount = 3,
        };

        return Task.FromResult(Result.Success(wrapper));
    }
}
