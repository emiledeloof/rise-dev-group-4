using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Rise.Persistence;
using Testcontainers.PostgreSql;

namespace Rise.Services.IntegrationTests.Infrastructure;

/// <summary>Starts one real postgres container for all the integration tests and resets its data between tests.</summary>
public class PostgresFixture : IAsyncLifetime
{
    // same image as docker-compose so the tests run on the production engine
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private Respawner respawner = null!;

    /// <summary>Starts the container, applies the migrations and prepares respawn.</summary>
    public async Task InitializeAsync()
    {
        await container.StartAsync();

        // migrations instead of EnsureCreated, so a broken migration also fails the tests
        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            // ! never empty this table, otherwise ef thinks no migration was applied
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>Creates a new short lived context, use one for arrange, one for act and one for assert.</summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Empties every table so each test starts from a clean database.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }

    /// <summary>Stops and removes the container.</summary>
    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}
