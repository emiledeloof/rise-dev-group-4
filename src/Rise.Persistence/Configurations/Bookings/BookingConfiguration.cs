using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Configurations.Bookings;

/// <summary>
/// Specific configuration for <see cref="Booking"/>.
/// </summary>
internal class BookingConfiguration : EntityConfiguration<Booking>
{
    public override void Configure(EntityTypeBuilder<Booking> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.BookingNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.Property(x => x.CheckIn).IsRequired();
        builder.Property(x => x.CheckOut).IsRequired();

        builder.HasOne(x => x.Guest)
            .WithMany(x => x.Bookings)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // The booked rooms belong to the booking, so they are removed together with the booking.
        builder.HasMany(x => x.Rooms)
            .WithOne(x => x.Booking)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Payments are financial records and should never be removed implicitly.
        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Booking)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
