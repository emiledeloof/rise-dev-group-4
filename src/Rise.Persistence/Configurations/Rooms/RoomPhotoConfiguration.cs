using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Rooms;

namespace Rise.Persistence.Configurations.Rooms;

/// <summary>
/// Specific configuration for <see cref="RoomPhoto"/>.
/// </summary>
internal class RoomPhotoConfiguration : EntityConfiguration<RoomPhoto>
{
    public override void Configure(EntityTypeBuilder<RoomPhoto> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Url).IsRequired();
    }
}
