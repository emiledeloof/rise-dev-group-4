using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Rooms;

namespace Rise.Persistence.Configurations.Rooms;

/// <summary>
/// Specific configuration for <see cref="Rate"/>.
/// </summary>
internal class RateConfiguration : EntityConfiguration<Rate>
{
    public override void Configure(EntityTypeBuilder<Rate> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        // sort by rate name on the admin rates page, low gain with few rates
        // ? the search uses Contains so it cant use this index
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.PricePerNight).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
