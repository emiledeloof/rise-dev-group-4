using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rise.Persistence.Configurations.Identity;

/// <summary>
/// Configuration for the Identity tables.
/// </summary>
internal class IdentityConfiguration :
    IEntityTypeConfiguration<IdentityUser>,
    IEntityTypeConfiguration<IdentityRole>,
    IEntityTypeConfiguration<IdentityUserRole<string>>,
    IEntityTypeConfiguration<IdentityUserClaim<string>>,
    IEntityTypeConfiguration<IdentityUserLogin<string>>,
    IEntityTypeConfiguration<IdentityRoleClaim<string>>,
    IEntityTypeConfiguration<IdentityUserToken<string>>
{
    // Configures the IdentityUser tables.
    // NOTE:
    // If you want to use a separate schema for your Identity tables,
    // you can specify the schema as "auth" like this:
    //     builder.ToTable("Users", "auth");
    // PostgreSQL supports schemas, but SQLite does NOT, so keep that in mind if the provider ever changes.
    // The default below uses the default schema (public) and works on any provider.

    public void Configure(EntityTypeBuilder<IdentityUser> builder)
        => builder.ToTable("Users");

    public void Configure(EntityTypeBuilder<IdentityRole> builder)
        => builder.ToTable("Roles");

    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
        => builder.ToTable("UserRoles");

    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
        => builder.ToTable("UserClaims");

    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
        => builder.ToTable("UserLogins");

    public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> builder)
        => builder.ToTable("RoleClaims");

    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
        => builder.ToTable("UserTokens");
}