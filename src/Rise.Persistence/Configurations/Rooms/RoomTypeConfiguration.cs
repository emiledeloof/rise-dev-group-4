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
    }
}
