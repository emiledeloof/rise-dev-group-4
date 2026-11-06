using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Configurations.Bookings;

/// <summary>
/// Specific configuration for <see cref="BillLine"/>.
/// </summary>
internal class BillLineConfiguration : EntityConfiguration<BillLine>
{
    public override void Configure(EntityTypeBuilder<BillLine> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(BillLine.DescriptionMaxLength);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPrice).IsRequired();
        builder.Property(x => x.VatPercentage).IsRequired().HasPrecision(5, 2);
        builder.Ignore(x => x.Total);
    }
}
