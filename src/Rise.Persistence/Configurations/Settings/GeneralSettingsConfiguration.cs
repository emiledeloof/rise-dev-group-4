using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Settings;

namespace Rise.Persistence.Configurations.Settings;

internal class GeneralSettingsConfiguration : EntityConfiguration<GeneralSettings>
{
    public override void Configure(EntityTypeBuilder<GeneralSettings> builder)
    {
        base.Configure(builder);
        builder.Property(settings => settings.IsVatEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(settings => settings.VatPercentage).IsRequired().HasPrecision(5, 2).HasDefaultValue(0m);
    }
}
