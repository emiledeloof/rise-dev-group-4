namespace Rise.Shared.Settings;

public class GeneralSettingsDto
{
    public required bool IsVatEnabled { get; set; }
    public required decimal VatPercentage { get; set; }
}
