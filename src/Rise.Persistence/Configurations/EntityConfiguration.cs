using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Common;

namespace Rise.Persistence.Configurations;

/// <summary>
/// Base configuration for an <see cref="Entity"/>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
internal class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // All tables are singlular named and have the name of the class e.g. Room instead of Rooms.
        builder.ToTable(typeof(TEntity).Name);

        // CreatedAt should be filled in by the database when using raw SQL.
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); // Standard SQL, supported by PostgreSQL. Check this when moving to another database provider.

        // UpdatedAt should be filled in by the database when using raw SQL.
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); // Standard SQL, supported by PostgreSQL. Check this when moving to another database provider.

        // IsDeleted should be false by default, used for softdelete.
        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);
    }
}