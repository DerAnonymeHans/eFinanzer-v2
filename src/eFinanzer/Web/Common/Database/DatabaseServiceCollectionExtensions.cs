using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Common.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<EFinanzerDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql
                // Registers the enums with the Npgsql data source and creates them as postgres enum types
                .MapEnum<PaymentType>("payment_type")
                .MapEnum<ReceiptPositionState>("receipt_position_state")
                .MapEnum<Receipt.Type>("receipt_type"))
            // Changes are written with ExecuteUpdate/ExecuteDelete or Add, queries that need tracking opt in via AsTracking()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        return services;
    }
}
