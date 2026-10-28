namespace Rise.Domain.Settings;

public class GeneralSettings : Entity
{
    public const int SingletonId = 1;

    public bool IsVatEnabled { get; private set; }
    public decimal VatPercentage { get; private set; }

    private GeneralSettings()
    {
    }

    public GeneralSettings(bool isVatEnabled, decimal vatPercentage)
    {
        Id = SingletonId;
        Update(isVatEnabled, vatPercentage);
    }

    public void Update(bool isVatEnabled, decimal vatPercentage)
    {
        IsVatEnabled = isVatEnabled;
        VatPercentage = Guard.Against.OutOfRange(vatPercentage, nameof(vatPercentage), 0m, 100m);
    }
}
