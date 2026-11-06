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

        // ! derived from Rooms, without this EF sees a 2nd relationship to BookingRoom
        builder.Ignore(x => x.ActiveRooms);

        builder.Property(x => x.BookingNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.Property(x => x.CheckIn).IsRequired();
        builder.Property(x => x.CheckOut).IsRequired();

        // * composite index on the stay dates, used by the availability search in RoomService
        // not unique, several bookings can start or end the same day
        // ! order matters, checkin first then checkout
        builder.HasIndex(x => new { x.CheckIn, x.CheckOut });

        // sort by checkout on the admin booking list, low gain
        builder.HasIndex(x => x.CheckOut);

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

        // Bill lines are financial records too, never removed implicitly.
        builder.HasMany(x => x.BillLines)
            .WithOne(x => x.Booking)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
