namespace Rise.Shared.PromotionCodes;

public static class PromotionCodeRequest
{
    public class Create
    {
        public string Code { get; set; } = string.Empty;
        public PromotionCodeDiscountType DiscountType { get; set; }
        public decimal Value { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public string? EmailRestriction { get; set; }
        public PromotionCodeAppliesTo AppliesTo { get; set; }
        public bool IsActive { get; set; } = true;

        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                RuleFor(x => x.Code)
                    .NotEmpty()
                    .Must(code => !string.IsNullOrWhiteSpace(code))
                    .WithMessage("Code cannot contain only whitespace.")
                    .Matches("^[A-Za-z0-9_-]+$")
                    .WithMessage("Code may contain only letters, numbers, hyphens and underscores.")
                    .MaximumLength(50);
                RuleFor(x => x.Value)
                    .GreaterThan(0);
                RuleFor(x => x.Value)
                    .LessThanOrEqualTo(100_000);
                RuleFor(x => x.ValidFrom)
                    .NotEmpty()
                    .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                    .WithMessage("Start date cannot be in the past.");
                RuleFor(x => x.ValidTo)
                    .GreaterThanOrEqualTo(x => x.ValidFrom);
                RuleFor(x => x.EmailRestriction)
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
