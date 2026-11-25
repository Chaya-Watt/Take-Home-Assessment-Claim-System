using System.Security.Claims;
using ClaimSystem.Api.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaimSystem.Api.Claims;

/// <summary>Staff (and supervisor) endpoints: work queue, pick up a claim, request information, assess.</summary>
public static class StaffClaimEndpoints
{
    public static IEndpointRouteBuilder MapStaffClaimEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staff/claims")
            .WithTags("Claims (staff)")
            .RequireAuthorization(AuthorizationPolicies.StaffOrSupervisor);

        group.MapGet("/", List);
        group.MapGet("/{claimId:guid}", Get);
        group.MapPost("/{claimId:guid}/assign", AssignToMe);
        group.MapPost("/{claimId:guid}/information-requests", RequestInformation);
        group.MapPost("/{claimId:guid}/assessment", Assess);

        return app;
    }

    /// <summary>queue=Unassigned (waiting to be picked up) or Mine; omit for all claims.</summary>
    private static async Task<Ok<PagedResult<StaffClaimSummaryResponse>>> List(
        ClaimsPrincipal user, StaffClaimService claims, CancellationToken cancellationToken,
        StaffClaimQueue? queue = null, ClaimStatus? status = null, int page = 1, int pageSize = 20) =>
        TypedResults.Ok(await claims.ListAsync(user.GetUserId(), queue, status, page, pageSize, cancellationToken));

    private static async Task<Ok<StaffClaimDetailResponse>> Get(
        Guid claimId, StaffClaimService claims, CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.GetAsync(claimId, cancellationToken));

    private static async Task<Ok<StaffClaimDetailResponse>> AssignToMe(
        Guid claimId, ClaimsPrincipal user, StaffClaimService claims, CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.AssignToMeAsync(user.GetUserId(), claimId, cancellationToken));

    private static async Task<Ok<StaffClaimDetailResponse>> RequestInformation(
        Guid claimId, RequestInformationRequest request, ClaimsPrincipal user, StaffClaimService claims,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.RequestInformationAsync(user.GetUserId(), claimId, request, cancellationToken));

    private static async Task<Ok<StaffClaimDetailResponse>> Assess(
        Guid claimId, AssessClaimRequest request, ClaimsPrincipal user, StaffClaimService claims,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await claims.AssessAsync(user.GetUserId(), claimId, request, cancellationToken));
}
