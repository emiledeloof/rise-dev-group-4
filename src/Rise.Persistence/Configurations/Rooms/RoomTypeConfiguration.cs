using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Rooms;

namespace Rise.Persistence.Configurations.Rooms;

/// <summary>
/// specific configuration for <see cref="RoomType"/>
/// </summary>
internal class RoomTypeConfiguration : EntityConfiguration<RoomType>
{
    /// <summary>
    /// column rules of the room type table
    /// </summary>
    public override void Configure(EntityTypeBuilder<RoomType> builder)
    {
        base.Configure(builder);
        // same limits as the validators in the shared project
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);

        // amenities are shared by the room types, the RoomTypeAmenity join table only links them
        builder.HasMany(x => x.Amenities)
            .WithMany()
            .UsingEntity("RoomTypeAmenity");
        builder.Navigation(x => x.Amenities).UsePropertyAccessMode(PropertyAccessMode.Field);

        // texts per language, one row per language, removed together with the room type
        builder.OwnsMany(x => x.Translations, translation =>
        {
            translation.ToTable("RoomTypeTranslation");
            translation.WithOwner().HasForeignKey("RoomTypeId");
            translation.HasKey("RoomTypeId", nameof(RoomTypeTranslation.Language));
            translation.Property(x => x.Language).HasMaxLength(2);
            translation.Property(x => x.BedConfiguration).IsRequired().HasMaxLength(100);
        });
        builder.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
