namespace Rise.Domain.Promotions;

public class PromotionCode : Entity
{
    private string _code = string.Empty;
    public string Code
    {
        get => _code;
        set
        {
            var trimmed = Guard.Against.NullOrWhiteSpace(value).Trim();
            Guard.Against.StringTooLong(trimmed, 50, nameof(Code));
            Guard.Against.InvalidFormat(trimmed, nameof(Code), @"^[A-Za-z0-9_-]+$", "Code may contain only letters, numbers, hyphens and underscores.");
            _code = trimmed;
        }
    }

    private PromotionDiscountType _discountType;
    public PromotionDiscountType DiscountType
    {
        get => _discountType;
        set
        {
            if (value == PromotionDiscountType.Percentage && _value > 100)
            {
                Guard.Against.OutOfRange(_value, nameof(Value), 0.0001m, 100m, "A percentage discount cannot exceed 100.");
            }
            _discountType = value;
        }
    }

    private decimal _value;
    public decimal Value
    {
        get => _value;
        set
        {
            Guard.Against.NegativeOrZero(value);
            if (DiscountType == PromotionDiscountType.Percentage)
            {
                Guard.Against.OutOfRange(value, nameof(Value), 0.0001m, 100m, "A percentage discount cannot exceed 100.");
            }
            _value = value;
        }
    }

    private DateOnly _validFrom;
    public DateOnly ValidFrom
    {
        get => _validFrom;
        set
        {
            if (_validTo != default)
            {
                Guard.Against.InvalidInput(value, nameof(ValidFrom), from => from <= _validTo, "ValidFrom must be on or before ValidTo.");
            }
            _validFrom = value;
        }
    }

    private DateOnly _validTo;
    public DateOnly ValidTo
    {
        get => _validTo;
        set
        {
            Guard.Against.InvalidInput(value, nameof(ValidTo), to => to >= ValidFrom, "ValidTo must be on or after ValidFrom.");
            _validTo = value;
        }
    }

    public string? EmailRestriction { get; set; }
    public PromotionAppliesTo AppliesTo { get; set; }
    public bool IsActive { get; set; } = true;

    private PromotionCode()
    {
    }

    public PromotionCode(
        string code,
        PromotionDiscountType discountType,
        decimal value,
        DateOnly validFrom,
        DateOnly validTo,
        string? emailRestriction,
        PromotionAppliesTo appliesTo)
    {
        DiscountType = discountType;
        Code = code;
        Value = value;
        ValidFrom = validFrom;
        ValidTo = validTo;
        EmailRestriction = emailRestriction;
        AppliesTo = appliesTo;
    }
}
