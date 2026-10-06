using Rise.Domain.Guests;

namespace Rise.Domain.Tests.Guests;

public class GuestShould
{
    private static GuestAddress ValidAddress => new("Koningstraat 12", null, "Belgium");

    [Fact]
    public void BeCreated()
    {
        var guest = new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", ValidAddress);

        guest.Name.ShouldBe("Jan Janssens");
        guest.Email.ShouldBe("jan@example.com");
        guest.Address.ShouldBe(ValidAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidName(string? name)
    {
        Action act = () => new Guest(name!, "jan@example.com", "+32 470 12 34 56", ValidAddress);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidEmail(string? email)
    {
        Action act = () => new Guest("Jan Janssens", email!, "+32 470 12 34 56", ValidAddress);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidPhone(string? phone)
    {
        Action act = () => new Guest("Jan Janssens", "jan@example.com", phone!, ValidAddress);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void NotBeCreatedWithoutAnAddress()
    {
        Action act = () => new Guest("Jan Janssens", "jan@example.com", "+32 470 12 34 56", null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, "Belgium")]
    [InlineData("  ", "Belgium")]
    [InlineData("Koningstraat 12", null)]
    [InlineData("Koningstraat 12", "  ")]
    public void NotHaveAnAddressWithMissingRequiredFields(string? addressLine1, string? country)
    {
        Action act = () => new GuestAddress(addressLine1!, null, country!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void HaveEqualAddressesWhenAllFieldsAreEqual()
    {
        var address1 = new GuestAddress("Koningstraat 12", "Bus 3A", "Belgium");
        var address2 = new GuestAddress("Koningstraat 12", "Bus 3A", "Belgium");

        address1.ShouldBe(address2);
    }
}
