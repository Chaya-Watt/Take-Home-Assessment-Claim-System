using ClaimSystem.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.Application;

/// <summary>Registers Application-layer services (use cases).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        return services;
    }
}
