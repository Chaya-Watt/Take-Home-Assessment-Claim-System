using ClaimSystem.Application.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.Application;

/// <summary>Registers Application-layer services (use cases).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PolicyService>();
        services.AddScoped<ClaimantClaimService>();
        return services;
    }
}
