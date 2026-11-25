using ClaimSystem.Domain.Users;

namespace ClaimSystem.Api.Auth;

/// <summary>Role-based policies used by endpoint groups via <c>RequireAuthorization(policy)</c>.</summary>
public static class AuthorizationPolicies
{
    public const string ClaimantOnly = nameof(ClaimantOnly);
    public const string StaffOrSupervisor = nameof(StaffOrSupervisor);
    public const string SupervisorOnly = nameof(SupervisorOnly);

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(ClaimantOnly, policy => policy.RequireRole(nameof(UserRole.Claimant)))
            .AddPolicy(StaffOrSupervisor, policy => policy.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Supervisor)))
            .AddPolicy(SupervisorOnly, policy => policy.RequireRole(nameof(UserRole.Supervisor)));
        return services;
    }
}
