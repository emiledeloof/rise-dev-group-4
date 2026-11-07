using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Rooms;

namespace Rise.Persistence.Configurations.Rooms;

/// <summary>
/// Specific configuration for <see cref="Room"/>.
/// </summary>
internal class RoomConfiguration : EntityConfiguration<Room>
{
    public override void Configure(EntityTypeBuilder<Room> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.RoomNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.RoomNumber).IsUnique();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        // lookup by room name in RateService and sorts by name, low gain with few rooms
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.MaxGuests).IsRequired();
        builder.Property(x => x.View).HasMaxLength(100);

        // Photos belong to the room, so they are removed together with the room.
        builder.HasMany(x => x.Photos)
            .WithOne(x => x.Room)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ! restrict, a type still used by a room must never disappear with it
        builder.HasOne(x => x.RoomType)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Rates are referenced by bookings, so they can't be removed together with the room.
        builder.HasMany(x => x.Rates)
            .WithOne(x => x.Room)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
