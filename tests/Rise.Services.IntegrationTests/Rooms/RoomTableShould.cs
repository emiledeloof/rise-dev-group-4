using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rise.Services.IntegrationTests.Infrastructure;

namespace Rise.Services.IntegrationTests.Rooms;

/// <summary>
/// rules the database itself enforces on the room table, on top of the domain guards
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RoomTableShould(PostgresFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RefuseARoomWithoutType()
    {
        await using var dbContext = database.CreateDbContext();

        // raw sql, the domain constructor already refuses a null type so only sql can try it
        var insert = () => dbContext.Database.ExecuteSqlRawAsync(
            """INSERT INTO "Room" ("RoomNumber", "Name", "MaxGuests") VALUES ('101', 'Standard Room', 2)""");

        var exception = await insert.ShouldThrowAsync<PostgresException>();
        exception.SqlState.ShouldBe(PostgresErrorCodes.NotNullViolation);
    }
}
