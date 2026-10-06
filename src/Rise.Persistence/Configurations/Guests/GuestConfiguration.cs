using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Guests;

namespace Rise.Persistence.Configurations.Guests;

/// <summary>
/// Specific configuration for <see cref="Guest"/>.
/// </summary>
internal class GuestConfiguration : EntityConfiguration<Guest>
{
    public override void Configure(EntityTypeBuilder<Guest> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(250);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);

        // Value Object Mapping and rename of columns
        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(x => x.AddressLine1).IsRequired().HasMaxLength(250).HasColumnName(nameof(GuestAddress.AddressLine1));
            address.Property(x => x.AddressLine2).HasMaxLength(250).HasColumnName(nameof(GuestAddress.AddressLine2));
            address.Property(x => x.Country).IsRequired().HasMaxLength(100).HasColumnName(nameof(GuestAddress.Country));
        }).Navigation(x => x.Address).IsRequired();
    }
}
