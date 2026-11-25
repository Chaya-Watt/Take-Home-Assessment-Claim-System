using ClaimSystem.Api.Auth;
using ClaimSystem.Application.Supervisor;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaimSystem.Api.Supervisor;

public static class SupervisorEndpoints
{
    public static IEndpointRouteBuilder MapSupervisorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/supervisor")
            .WithTags("Supervisor")
            .RequireAuthorization(AuthorizationPolicies.SupervisorOnly);

        group.MapGet("/dashboard", GetDashboard);

        return app;
    }

    /// <summary>Claims per status, unassigned claims, longest-waiting claims, and each staff member's open claims.</summary>
    private static async Task<Ok<SupervisorDashboardResponse>> GetDashboard(
        SupervisorDashboardService dashboard, CancellationToken cancellationToken) =>
        TypedResults.Ok(await dashboard.GetAsync(cancellationToken));
}
