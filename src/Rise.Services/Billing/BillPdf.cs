using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rise.Shared.Billing;

namespace Rise.Services.Billing;

/// <summary>turns the final bill into a pdf document, same numbers as the bill page</summary>
public static class BillPdf
{
    // TODO: add the legal details of the estate, address and vat number, ask the client
    private const string EstateName = "Stazzu La Vigna di Maria";

    // * free for a small company, set here so the tests do not depend on the startup of the server
    static BillPdf() => QuestPDF.Settings.License = LicenseType.Community;

    /// <summary>creates the pdf of the bill, returns the bytes of the file</summary>
    public static byte[] Create(BillingDto.Bill bill)
    {
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().Element(header => ComposeHeader(header, bill));
            page.Content().PaddingVertical(20).Element(content => ComposeContent(content, bill));
            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, BillingDto.Bill bill)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(EstateName).FontSize(18).SemiBold();
                column.Item().Text("Final bill").FontSize(12);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Booking {bill.BookingNumber}").SemiBold();
                column.Item().Text($"Guest: {bill.GuestName}");
                column.Item().Text($"Stay: {Date(bill.CheckIn)} to {Date(bill.CheckOut)} ({bill.Nights} nights)");
                // a bill that is not settled yet is only a draft for the guest
                column.Item().Text(bill.SettledAt is { } settledAt
                    ? $"Settled on {Date(DateOnly.FromDateTime(settledAt))}"
                    : "Not settled yet");
            });
        });
    }

    private static void ComposeContent(IContainer container, BillingDto.Bill bill)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            column.Item().Text("Accommodation").SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                HeaderRow(table, "Room", "Rate", "Nights", "Per night", "VAT", "Total");
                foreach (var line in bill.Accommodation)
                {
                    Cell(table, $"{line.RoomNumber} {line.RoomName}");
                    Cell(table, line.RateName);
                    Cell(table, line.Nights.ToString(CultureInfo.InvariantCulture), alignRight: true);
                    Cell(table, Euro(line.PricePerNight), alignRight: true);
                    Cell(table, Percent(line.VatPercentage), alignRight: true);
                    Cell(table, Euro(line.Total), alignRight: true);
                }
            });

            if (bill.OtherServices.Count > 0)
            {
                column.Item().Text("Other services").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(5);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    HeaderRow(table, "Description", "Qty", "Unit price", "VAT", "Total");
                    foreach (var line in bill.OtherServices)
                    {
                        Cell(table, line.Description);
                        Cell(table, line.Quantity.ToString(CultureInfo.InvariantCulture), alignRight: true);
                        Cell(table, Euro(line.UnitPrice), alignRight: true);
                        Cell(table, Percent(line.VatPercentage), alignRight: true);
                        Cell(table, Euro(line.Total), alignRight: true);
                    }
                });
            }

            column.Item().AlignRight().Width(250).Element(totals => ComposeTotals(totals, bill));
        });
    }

    private static void ComposeTotals(IContainer container, BillingDto.Bill bill)
    {
        container.Column(column =>
        {
            TotalLine(column, "Net amount", bill.NetAmount);
            // * one line per vat pct, same groups as the bill page
            foreach (var group in bill.VatGroups.Where(g => g.VatAmount != 0))
                TotalLine(column, $"VAT {Percent(group.VatPercentage)}", group.VatAmount);
            TotalLine(column, "Total", bill.Total, isBold: true);
            TotalLine(column, "Paid", bill.PaidAmount);
            TotalLine(column, "Balance due", bill.OutstandingBalance, isBold: true);
        });
    }

    private static void HeaderRow(TableDescriptor table, params string[] titles)
    {
        foreach (var title in titles)
            table.Cell().BorderBottom(1).PaddingBottom(4).Text(title).SemiBold();
    }

    private static void Cell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().PaddingVertical(3);
        (alignRight ? cell.AlignRight() : cell).Text(text);
    }

    private static void TotalLine(ColumnDescriptor column, string label, decimal amount, bool isBold = false)
    {
        column.Item().Row(row =>
        {
            var labelText = row.RelativeItem().Text(label);
            var amountText = row.AutoItem().Text(Euro(amount));
            if (isBold)
            {
                labelText.SemiBold();
                amountText.SemiBold();
            }
        });
    }

    // same format as the site, eg €1,250.00
    private static string Euro(decimal amount) => "€" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private static string Percent(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture) + " %";

    private static string Date(DateOnly date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
