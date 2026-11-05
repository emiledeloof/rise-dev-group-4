namespace Rise.Services.IntegrationTests.Infrastructure;

/// <summary>Groups the test classes that share the postgres container.</summary>
// * tests in the same collection never run in parallel, so they cannot reset each other's data
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary>Name to put in the Collection attribute of a test class.</summary>
    public const string Name = "Database";
}
