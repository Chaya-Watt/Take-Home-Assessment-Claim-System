using ClaimSystem.Application.Claims;
using ClaimSystem.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ClaimSystem.Infrastructure;

/// <summary>Registers Infrastructure services (DbContext, JWT, Kafka, ...).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ClaimDbContext>((sp, options) =>
        {
            var connectionString = new SqliteConnectionStringBuilder(configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured."));

            // A relative path is resolved against the Api project folder (content root),
            // so `dotnet run` and the integration tests use the same claims.db.
            connectionString.DataSource = Path.Combine(
                sp.GetRequiredService<IHostEnvironment>().ContentRootPath, connectionString.DataSource);

            options.UseSqlite(connectionString.ToString());
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();

        return services;
    }
}
