namespace Rise.Shared.Settings;

public static class GeneralSettingsRequest
{
    public class Update
    {
        public bool IsVatEnabled { get; set; }
        public decimal VatPercentage { get; set; }

        public class Validator : AbstractValidator<Update>
        {
            public Validator()
            {
                RuleFor(settings => settings.VatPercentage)
                    .InclusiveBetween(0m, 100m)
                    .WithMessage("VAT percentage must be between 0 and 100%.")
                    .PrecisionScale(5, 2, true)
                    .WithMessage("VAT percentage can have at most 2 decimal places.");
            }
        }
    }
}
