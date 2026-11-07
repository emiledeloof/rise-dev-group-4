using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Guests;
using Rise.Persistence;
using Rise.Services.Guests;
using Rise.Shared.Guests;
using Shouldly;

namespace Rise.Services.Tests.Guests;

/// <summary>
/// Unit tests for <see cref="GuestService"/>.
/// Verifies business rules, persistence logic, and conflict detection using an EF Core InMemory database.
/// Follows naming convention &lt;Subject&gt;Should.&lt;Behavior&gt;.
/// </summary>
public class GuestServiceShould
{
    [Fact]
    public async Task CreateGuestWhenValid()
    {
        // Arrange
        // Use a unique in-memory database name per test to ensure test isolation
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(CreateGuestWhenValid))
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var service = new GuestService(dbContext);

        var request = new GuestRequest.Create
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+32 123 456 789",
            AddressLine1 = "Kerkstraat 1",
            AddressLine2 = "Bus 2",
            Country = "Belgium"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        // 1. Result assertion
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.ShouldNotBeNull();
        result.Value.GuestId.ShouldBeGreaterThan(0);

        // 2. Database state assertion
        var savedGuest = await dbContext.Guests.FirstOrDefaultAsync(g => g.Id == result.Value.GuestId);
        savedGuest.ShouldNotBeNull();
        savedGuest.Name.ShouldBe("John Doe");
        savedGuest.Email.ShouldBe("john.doe@example.com");
        savedGuest.Phone.ShouldBe("+32 123 456 789");
        savedGuest.Address.ShouldNotBeNull();
        savedGuest.Address.AddressLine1.ShouldBe("Kerkstraat 1");
        savedGuest.Address.AddressLine2.ShouldBe("Bus 2");
        savedGuest.Address.Country.ShouldBe("Belgium");
    }

    [Fact]
    public async Task ReturnConflictWhenGuestEmailAlreadyExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(ReturnConflictWhenGuestEmailAlreadyExists))
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        // Seed an existing guest with the target email
        var existingAddress = new GuestAddress("Main Street 10", null, "Belgium");
        var existingGuest = new Guest("Existing Person", "existing.guest@example.com", "+32 987 654 321", existingAddress);
        dbContext.Guests.Add(existingGuest);
        await dbContext.SaveChangesAsync();

        var service = new GuestService(dbContext);

        // Attempt to create a new guest with the same email address
        var duplicateRequest = new GuestRequest.Create
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "existing.guest@example.com",
            PhoneNumber = "+32 111 222 333",
            AddressLine1 = "Other Street 5",
            Country = "Belgium"
        };

        // Act
        var result = await service.CreateAsync(duplicateRequest);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.ShouldNotBeEmpty();
        result.Errors.ShouldContain(e => e.Contains("already exists"));

        // Verify only 1 guest exists in the database
        var totalGuests = await dbContext.Guests.CountAsync();
        totalGuests.ShouldBe(1);
    }

    [Fact]
    public async Task TrimTheSpacesTypedAroundTheFields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(TrimTheSpacesTypedAroundTheFields))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var result = await new GuestService(dbContext).CreateAsync(new GuestRequest.Create
        {
            FirstName = "  John ",
            LastName = " Doe  ",
            Email = " john.doe@example.com ",
            PhoneNumber = " +32 123 456 789 ",
            AddressLine1 = "Kerkstraat 1",
            Country = "Belgium",
        });

        result.IsSuccess.ShouldBeTrue();
        var savedGuest = await dbContext.Guests.SingleAsync();
        savedGuest.Name.ShouldBe("John Doe");
        savedGuest.Email.ShouldBe("john.doe@example.com");
        savedGuest.Phone.ShouldBe("+32 123 456 789");
    }

    [Fact]
    public async Task ReturnConflictWhenTheSameEmailHasSpacesAround()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(ReturnConflictWhenTheSameEmailHasSpacesAround))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        dbContext.Guests.Add(new Guest("Existing Person", "existing.guest@example.com", "+32 987 654 321",
            new GuestAddress("Main Street 10", null, "Belgium")));
        await dbContext.SaveChangesAsync();

        // * the email is trimmed before the check, so a space typed by mistake does not create a second guest
        var result = await new GuestService(dbContext).CreateAsync(new GuestRequest.Create
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "  existing.guest@example.com ",
            PhoneNumber = "+32 111 222 333",
            AddressLine1 = "Other Street 5",
            Country = "Belgium",
        });

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await dbContext.Guests.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task KeepTheOptionalSecondAddressLineEmpty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(KeepTheOptionalSecondAddressLineEmpty))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var result = await new GuestService(dbContext).CreateAsync(new GuestRequest.Create
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+32 123 456 789",
            AddressLine1 = "Kerkstraat 1",
            AddressLine2 = null,
            Country = "Belgium",
        });

        result.IsSuccess.ShouldBeTrue();
        (await dbContext.Guests.SingleAsync()).Address.AddressLine2.ShouldBeNull();
    }
}
