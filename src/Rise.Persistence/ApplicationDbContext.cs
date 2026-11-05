using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Admins;
using Rise.Domain.Bookings;
using Rise.Domain.ExtraServices;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;

namespace Rise.Persistence;

/// <summary>
/// Entrance to the database, inherits from IdentityDbContext and is basically a Unit Of Work and Repository pattern combined.
/// A <see cref="DbSet"/> is a repository for a specific type of entity.
/// The <see cref="ApplicationDbContext"/> is the Unit Of Work pattern
/// Will look very similar when switching database providers.
/// See https://hogent-web.github.io/csharp/chapters/09/slides/index.html#1
/// See https://enterprisecraftsmanship.com/posts/should-you-abstract-database/
/// </summary>
/// <param name="opts"></param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : IdentityDbContext<IdentityUser>(opts)
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<RoomPhoto> RoomPhotos => Set<RoomPhoto>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<Rate> Rates => Set<Rate>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<GeneralSettings> GeneralSettings => Set<GeneralSettings>();
    public DbSet<ExtraService> ExtraServices => Set<ExtraService>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All columns in the database have a maxlength of 4000.
        // in NVARACHAR 4000 is the maximum length that can be indexed by a database.
        // Some columns need more length, but these can be set on the configuration level for that Entity in particular.
        configurationBuilder.Properties<string>().HaveMaxLength(4_000);
        // All decimals columns should have 2 digits after the comma
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Applying all types of IEntityTypeConfiguration in the Persistence project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
