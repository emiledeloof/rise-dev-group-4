using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Configurations.Bookings;

/// <summary>
/// Specific configuration for <see cref="BookingRoom"/>.
/// </summary>
internal class BookingRoomConfiguration : EntityConfiguration<BookingRoom>
{
    public override void Configure(EntityTypeBuilder<BookingRoom> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.PricePerNight).IsRequired();

        builder.HasOne(x => x.Room)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Rate)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
