using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Admins;

namespace Rise.Persistence.Configurations.Admins;

/// <summary>
/// Specific configuration for <see cref="Admin"/>.
/// </summary>
internal class AdminConfiguration : EntityConfiguration<Admin>
{
    public override void Configure(EntityTypeBuilder<Admin> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.AccountId).IsRequired().HasMaxLength(36);
        builder.HasIndex(x => x.AccountId).IsUnique();
    }
}
