using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Promotions;

namespace Rise.Persistence.Configurations.Promotions;

internal class PromotionCodeConfiguration : EntityConfiguration<PromotionCode>
{
    public override void Configure(EntityTypeBuilder<PromotionCode> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.DiscountType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo).IsRequired();
        builder.Property(x => x.EmailRestriction).HasMaxLength(320);
        builder.Property(x => x.AppliesTo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
    }
}
