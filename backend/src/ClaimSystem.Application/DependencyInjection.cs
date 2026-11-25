using ClaimSystem.Application.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Policies;
using ClaimSystem.Application.Supervisor;
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
        services.AddScoped<StaffClaimService>();
        services.AddScoped<SupervisorDashboardService>();
        return services;
    }
}
