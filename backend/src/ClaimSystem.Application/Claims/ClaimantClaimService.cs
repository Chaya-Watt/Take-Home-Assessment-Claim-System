using ClaimSystem.Application.Common;
using ClaimSystem.Application.Policies;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;

namespace ClaimSystem.Application.Claims;

/// <summary>
/// Claim use cases for the claimant. A claim that belongs to someone else is reported as "not found",
/// so claimants cannot even learn that it exists.
/// </summary>
public sealed class ClaimantClaimService(
    IClaimRepository claims,
    IPolicyRepository policies,
    IClaimNumberGenerator claimNumbers,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<ClaimDetailResponse> SubmitAsync(
        Guid claimantId, SubmitClaimRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await policies.FindByIdAsync(request.PolicyId, cancellationToken)
            ?? throw NotFound("Policy not found.");

        var now = Now();
        var claimNumber = await claimNumbers.NextAsync(now, cancellationToken);
        var claim = Claim.Submit(
            claimNumber, claimantId, policy, request.IncidentType, request.IncidentDate,
            request.Location, request.Description, request.ClaimedAmount, now);

        claims.Add(claim);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ClaimDetailResponse.From(claim, policy.PolicyNumber);
    }

    public async Task<PagedResult<ClaimSummaryResponse>> ListMineAsync(
        Guid claimantId, ClaimStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PagedResult<ClaimSummaryResponse>.MaxPageSize);

        var result = await claims.ListByClaimantAsync(claimantId, status, page, pageSize, cancellationToken);
        return result.Map(ClaimSummaryResponse.From);
    }

    public async Task<ClaimDetailResponse> GetAsync(Guid claimantId, Guid claimId, CancellationToken cancellationToken = default)
    {
        var claim = await LoadOwnedAsync(claimantId, claimId, cancellationToken);
        return await ToDetailAsync(claim, cancellationToken);
    }

    public async Task<ClaimDetailResponse> WithdrawAsync(Guid claimantId, Guid claimId, CancellationToken cancellationToken = default)
    {
        var claim = await LoadOwnedAsync(claimantId, claimId, cancellationToken);
        claim.Withdraw(claimantId, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDetailAsync(claim, cancellationToken);
    }

    private async Task<Claim> LoadOwnedAsync(Guid claimantId, Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await claims.FindByIdAsync(claimId, cancellationToken);
        return claim is not null && claim.ClaimantId == claimantId ? claim : throw NotFound("Claim not found.");
    }

    private async Task<ClaimDetailResponse> ToDetailAsync(Claim claim, CancellationToken cancellationToken)
    {
        var policy = await policies.FindByIdAsync(claim.PolicyId, cancellationToken);
        return ClaimDetailResponse.From(claim, policy?.PolicyNumber ?? string.Empty);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static DomainException NotFound(string message) => new(DomainErrors.NotFound, message);
}
