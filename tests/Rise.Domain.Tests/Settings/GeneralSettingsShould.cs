using Rise.Domain.Settings;

namespace Rise.Domain.Tests.Settings;

/// <summary>
/// tests for <see cref="GeneralSettings"/>, the vat used by every price of the site
/// </summary>
public class GeneralSettingsShould
{
    [Fact]
    public void BeTheOnlyRowOfItsTable()
    {
        var settings = new GeneralSettings(true, 10m);

        // * the services always read the row with this id
        settings.Id.ShouldBe(GeneralSettings.SingletonId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void AcceptAVatPercentageFromZeroToHundred(decimal vatPercentage)
    {
        var settings = new GeneralSettings(true, vatPercentage);

        settings.VatPercentage.ShouldBe(vatPercentage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void RefuseAVatPercentageOutOfRange(decimal vatPercentage)
    {
        Action act = () => new GeneralSettings(true, vatPercentage);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void RefuseAnUpdateOutOfRange()
    {
        var settings = new GeneralSettings(true, 10m);

        Action act = () => settings.Update(true, 150m);

        act.ShouldThrow<ArgumentException>();
        settings.VatPercentage.ShouldBe(10m);
    }

    [Fact]
    public void ChangeBothValuesOnUpdate()
    {
        var settings = new GeneralSettings(false, 0m);

        settings.Update(true, 22m);

        settings.IsVatEnabled.ShouldBeTrue();
        settings.VatPercentage.ShouldBe(22m);
    }
}
