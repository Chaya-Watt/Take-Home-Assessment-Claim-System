using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.Application;

/// <summary>Registers Application-layer services (use cases). Filled in as features are added.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
