using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Rooms;

namespace Rise.Persistence.Configurations.Rooms;

/// <summary>
/// Specific configuration for <see cref="Amenity"/>.
/// </summary>
internal class AmenityConfiguration : EntityConfiguration<Amenity>
{
    public override void Configure(EntityTypeBuilder<Amenity> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        // one row per amenity, so no "Wifi" next to "Wi-Fi"
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Icon).HasMaxLength(50);
    }
}
