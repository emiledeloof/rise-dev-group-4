using Rise.Domain.Bookings;
using Rise.Domain.Guests;

namespace Rise.Domain.Tests.Bookings;

public class BillLineShould
{
    private static Booking CreateBooking()
        => new("20261006-0042",
            new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium")),
            new DateOnly(2026, 10, 10),
            new DateOnly(2026, 10, 12),
            2);

    [Fact]
    public void MultiplyTheUnitPriceByTheQuantity()
    {
        var line = CreateBooking().AddBillLine("Guided vineyard tour", 2, 45m, 22m);

        line.Total.ShouldBe(90m);
        line.VatPercentage.ShouldBe(22m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBeCreatedWithoutDescription(string? description)
    {
        Action act = () => CreateBooking().AddBillLine(description!, 1, 5m, 22m);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void NotBeCreatedWithATooLongDescription()
    {
        Action act = () => CreateBooking().AddBillLine(new string('a', BillLine.DescriptionMaxLength + 1), 1, 5m, 22m);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWithoutQuantity(int quantity)
    {
        Action act = () => CreateBooking().AddBillLine("Minibar", quantity, 5m, 22m);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NotBeCreatedWithoutPrice(decimal unitPrice)
    {
        Action act = () => CreateBooking().AddBillLine("Minibar", 1, unitPrice, 22m);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void NotBeCreatedWithAVatPercentageOutOfRange(decimal vatPercentage)
    {
        Action act = () => CreateBooking().AddBillLine("Minibar", 1, 5m, vatPercentage);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void BeUpdated()
    {
        var line = CreateBooking().AddBillLine("Minibar", 1, 5m, 22m);

        line.Update(" Wine tasting ", 2, 25m, 10m);

        line.Description.ShouldBe("Wine tasting");
        line.Quantity.ShouldBe(2);
        line.UnitPrice.ShouldBe(25m);
        line.VatPercentage.ShouldBe(10m);
        line.Total.ShouldBe(50m);
    }
}
