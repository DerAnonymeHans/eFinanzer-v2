using eFinanzer.Common.Database;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Base class for tests using the database. Every test starts with an empty database.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTest(DatabaseFixture database) : IAsyncLifetime
{
    protected DatabaseFixture Database { get; } = database;

    /// <summary>
    /// Context for arranging and asserting. The class under test gets its own one from
    /// <see cref="DatabaseFixture.CreateContext"/>.
    /// </summary>
    protected EFinanzerDbContext Context { get; private set; } = null!;

    public virtual async ValueTask InitializeAsync()
    {
        await Database.ResetAsync();
        Context = Database.CreateContext();
    }

    public virtual async ValueTask DisposeAsync() => await Context.DisposeAsync();
}
