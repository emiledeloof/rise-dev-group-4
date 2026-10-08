namespace Rise.Shared.Invoice;

public static partial class InvoiceResponse
{
    public class Index
    {
        public IEnumerable<InvoiceDto.Index> Invoices { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
