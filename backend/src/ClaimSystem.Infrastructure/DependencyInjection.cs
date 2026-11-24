using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.Infrastructure;

/// <summary>Registers Infrastructure services (DbContext, JWT, Kafka, ...). Filled in by later phases.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services;
    }
}
