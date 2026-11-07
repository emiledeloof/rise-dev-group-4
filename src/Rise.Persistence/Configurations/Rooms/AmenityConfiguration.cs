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
        builder.Property(x => x.Icon).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // the names live in their own table, one row per language, removed together with the amenity
        builder.OwnsMany(x => x.Translations, translation =>
        {
            translation.ToTable("AmenityTranslation");
            translation.WithOwner().HasForeignKey("AmenityId");
            translation.HasKey("AmenityId", nameof(AmenityTranslation.Language));
            translation.Property(x => x.Language).HasMaxLength(2);
            translation.Property(x => x.Name).IsRequired().HasMaxLength(100);
            // one amenity per name and language, so no "Wifi" next to "Wi-Fi", the service also checks the case
            translation.HasIndex(nameof(AmenityTranslation.Language), nameof(AmenityTranslation.Name)).IsUnique();
        });
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
