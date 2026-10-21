using eFinanzer.Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Postgres container with the migrated schema, shared by all tests of the <see cref="DatabaseCollection"/>
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    // Same major version as in production (see src/deploy/docker-compose.yml)
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private ServiceProvider _serviceProvider = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Uses the production registration, so the enum mappings match the ones of the app
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = ConnectionString })
            .Build();
        _serviceProvider = new ServiceCollection()
            .AddDatabase(configuration)
            .BuildServiceProvider();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// New context without tracked entities. Use separate ones for arranging, acting and asserting, so the
    /// assertions read what was actually written to the database.
    /// </summary>
    public EFinanzerDbContext CreateContext()
        => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<EFinanzerDbContext>();

    /// <summary>
    /// Deletes all data. Every table except the role change requests depends on the accounting units, so truncating
    /// them cascades to all others.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE \"AccountingUnits\" CASCADE");
        await context.RoleChangeRequests.ExecuteDeleteAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _container.DisposeAsync();
    }
}
