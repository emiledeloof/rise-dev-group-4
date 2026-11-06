namespace Rise.Domain.Promotions;

public class PromotionCode : Entity
{
    private string _code = string.Empty;
    public string Code
    {
        get => _code;
        set => _code = Guard.Against.NullOrWhiteSpace(value).Trim();
    }

    public PromotionDiscountType DiscountType { get; set; }

    private decimal _value;
    public decimal Value
    {
        get => _value;
        set => _value = Guard.Against.NegativeOrZero(value);
    }

    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
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
        Code = code;
        DiscountType = discountType;
        Value = value;
        ValidFrom = validFrom;
        ValidTo = validTo;
        EmailRestriction = emailRestriction;
        AppliesTo = appliesTo;
    }
}
