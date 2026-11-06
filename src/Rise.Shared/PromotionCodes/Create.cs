namespace Rise.Shared.PromotionCodes;

public static class PromotionCodeRequest
{
    private static readonly TimeZoneInfo EstateTimeZone = ResolveEstateTimeZone();

    private static TimeZoneInfo ResolveEstateTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    /// <summary>
    /// Current date in the estate's timezone (Sardinia - Europe/Rome).
    /// Keeps client and server in sync on what date "today" is regardless of host machine timezone.
    /// </summary>
    public static DateOnly GetCurrentDate() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, EstateTimeZone).DateTime);

    public class Create
    {
        public string Code { get; set; } = string.Empty;
        public PromotionCodeDiscountType DiscountType { get; set; } = PromotionCodeDiscountType.Percentage;
        public decimal Value { get; set; } = 10;
        public DateOnly ValidFrom { get; set; } = GetCurrentDate();
        public DateOnly ValidTo { get; set; } = GetCurrentDate().AddMonths(1);
        public string? EmailRestriction { get; set; }
        public PromotionCodeAppliesTo AppliesTo { get; set; } = PromotionCodeAppliesTo.NonRefundable;
        public bool IsActive { get; set; } = true;

        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                RuleFor(x => x.Code)
                    .NotEmpty()
                    .WithMessage("Code is required.")
                    .Matches("^[A-Za-z0-9_-]+$")
                    .WithMessage("Code may contain only letters, numbers, hyphens and underscores.")
                    .MaximumLength(50)
                    .WithMessage("Code cannot exceed 50 characters.");

                RuleFor(x => x.Value)
                    .GreaterThan(0)
                    .WithMessage("Value must be greater than 0.")
                    .LessThanOrEqualTo(100_000)
                    .WithMessage("Value cannot exceed 100,000.");

                RuleFor(x => x.ValidFrom)
                    .NotEmpty()
                    .GreaterThanOrEqualTo(_ => GetCurrentDate())
                    .WithMessage("Start date cannot be in the past.");

                RuleFor(x => x.ValidTo)
                    .GreaterThanOrEqualTo(x => x.ValidFrom)
                    .WithMessage("End date must be on or after the start date.");

                RuleFor(x => x.EmailRestriction)
                    .MaximumLength(320)
                    .WithMessage("Email restriction cannot exceed 320 characters.")
                    .EmailAddress()
                    .When(x => !string.IsNullOrWhiteSpace(x.EmailRestriction));

                RuleFor(x => x)
                    .Must(x => x.DiscountType != PromotionCodeDiscountType.Percentage || x.Value <= 100)
                    .WithMessage("A percentage discount cannot exceed 100.");

                RuleFor(x => x.DiscountType)
                    .IsInEnum();

                RuleFor(x => x.AppliesTo)
                    .IsInEnum();
            }
        }
    }
}

public enum PromotionCodeDiscountType
{
    Percentage,
    Fixed
}

public enum PromotionCodeAppliesTo
{
    NonRefundable,
    AllRates
}

public static class PromotionCodeResponse
{
    public class Create
    {
        public required int Id { get; init; }
        public required string Code { get; init; }
    }
}
