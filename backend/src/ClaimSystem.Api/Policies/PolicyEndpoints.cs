using System.Security.Claims;
using ClaimSystem.Api.Auth;
using ClaimSystem.Application.Policies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaimSystem.Api.Policies;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/policies")
            .WithTags("Policies")
            .RequireAuthorization(AuthorizationPolicies.ClaimantOnly);

        group.MapGet("/mine", ListMine);

        return app;
    }

    /// <summary>The signed-in claimant's policies, with the incident types each one covers.</summary>
    private static async Task<Ok<IReadOnlyList<PolicyResponse>>> ListMine(
        ClaimsPrincipal user, PolicyService policies, CancellationToken cancellationToken) =>
        TypedResults.Ok(await policies.ListMineAsync(user.GetUserId(), cancellationToken));
}
