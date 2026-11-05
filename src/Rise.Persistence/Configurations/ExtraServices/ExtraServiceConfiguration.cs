using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.ExtraServices;

namespace Rise.Persistence.Configurations.ExtraServices;

/// <summary>
/// Entity configuration for <see cref="ExtraService"/>.
/// </summary>
internal class ExtraServiceConfiguration : EntityConfiguration<ExtraService>
{
    public override void Configure(EntityTypeBuilder<ExtraService> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Price).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.Cadence).IsRequired();
        builder.Property(x => x.IsPerPerson).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
    }
}
