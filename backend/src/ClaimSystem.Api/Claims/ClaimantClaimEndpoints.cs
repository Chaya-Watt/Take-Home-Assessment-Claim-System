using System.Security.Claims;
using ClaimSystem.Api.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaimSystem.Api.Claims;

/// <summary>Claimant endpoints: report an incident, track own claims, withdraw.</summary>
public static class ClaimantClaimEndpoints
{
    public static IEndpointRouteBuilder MapClaimantClaimEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/claims")
            .WithTags("Claims (claimant)")
            .RequireAuthorization(AuthorizationPolicies.ClaimantOnly);

        group.MapPost("/", Submit);
        group.MapGet("/mine", ListMine);
        group.MapGet("/{claimId:guid}", Get);
        group.MapPost("/{claimId:guid}/withdraw", Withdraw);

        return app;
    }

    private static async Task<Created<ClaimDetailResponse>> Submit(
        SubmitClaimRequest request, ClaimsPrincipal user, ClaimantClaimService claims, CancellationToken cancellationToken)
    {
        var claim = await claims.SubmitAsync(user.GetUserId(), request, cancellationToken);
        return TypedResults.Created($"/api/claims/{claim.Id}", claim);
    }

    private static async Task<Ok<PagedResult<ClaimSummaryResponse>>> ListMine(
        ClaimsPrincipal user, ClaimantClaimService claims, CancellationToken cancellationToken,
        ClaimStatus? status = null, int page = 1, int pageSize = 20) =>
        TypedResults.Ok(await claims.ListMineAsync(user.GetUserId(), status, page, pageSize, cancellationToken));

    private static async Task<Ok<ClaimDetailResponse>> Get(
        Guid claimId, ClaimsPrincipal user, ClaimantClaimService claims, CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.GetAsync(user.GetUserId(), claimId, cancellationToken));

    private static async Task<Ok<ClaimDetailResponse>> Withdraw(
        Guid claimId, ClaimsPrincipal user, ClaimantClaimService claims, CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.WithdrawAsync(user.GetUserId(), claimId, cancellationToken));
}
