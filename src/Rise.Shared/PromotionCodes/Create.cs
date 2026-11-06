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
                    .MaximumLength(50);
                RuleFor(x => x.Value)
                    .GreaterThan(0);
                RuleFor(x => x.ValidFrom)
                    .NotEmpty();
                RuleFor(x => x.ValidTo)
                    .GreaterThanOrEqualTo(x => x.ValidFrom);
                RuleFor(x => x.EmailRestriction)
                    .EmailAddress()
                    .When(x => !string.IsNullOrWhiteSpace(x.EmailRestriction));
                RuleFor(x => x)
                    .Must(x => x.DiscountType != PromotionCodeDiscountType.Percentage || x.Value <= 100)
                    .WithMessage("A percentage discount cannot exceed 100.");
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
