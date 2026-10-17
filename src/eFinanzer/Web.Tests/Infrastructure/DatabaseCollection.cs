namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Tests sharing the database container. They run one after another, because every test resets the database.
/// </summary>
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
