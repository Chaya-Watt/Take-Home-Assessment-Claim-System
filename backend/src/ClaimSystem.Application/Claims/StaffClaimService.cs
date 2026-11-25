using ClaimSystem.Application.Common;
using ClaimSystem.Application.Policies;
using ClaimSystem.Application.Users;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;

namespace ClaimSystem.Application.Claims;

/// <summary>
/// Claim use cases for staff and supervisors. Any staff member can view any claim;
/// only the assigned staff member can act on it (enforced by the <see cref="Claim"/> aggregate).
/// </summary>
public sealed class StaffClaimService(
    IClaimRepository claims,
    IPolicyRepository policies,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<PagedResult<StaffClaimSummaryResponse>> ListAsync(
        Guid staffId, StaffClaimQueue? queue, ClaimStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<Claim>.Normalize(page, pageSize);
        var result = await claims.ListForStaffAsync(staffId, queue, status, page, pageSize, cancellationToken);
        return result.Map(StaffClaimSummaryResponse.From);
    }

    public async Task<StaffClaimDetailResponse> GetAsync(Guid claimId, CancellationToken cancellationToken = default) =>
        await ToDetailAsync(await LoadAsync(claimId, cancellationToken), cancellationToken);

    /// <summary>The caller picks up an unassigned claim. 409 if someone else already has it.</summary>
    public Task<StaffClaimDetailResponse> AssignToMeAsync(Guid staffId, Guid claimId, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.Assign(staffId, Now()), cancellationToken);

    public Task<StaffClaimDetailResponse> RequestInformationAsync(
        Guid staffId, Guid claimId, RequestInformationRequest request, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.RequestInformation(staffId, request.Message, request.DueDate, Now()), cancellationToken);

    public Task<StaffClaimDetailResponse> AssessAsync(
        Guid staffId, Guid claimId, AssessClaimRequest request, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.Assess(staffId, request.AssessedAmount, request.Summary, Now()), cancellationToken);

    public Task<StaffClaimDetailResponse> ApproveAsync(
        Guid staffId, Guid claimId, ApproveClaimRequest request, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.Approve(staffId, request.ApprovedAmount, request.Note, Now()), cancellationToken);

    public Task<StaffClaimDetailResponse> RejectAsync(
        Guid staffId, Guid claimId, RejectClaimRequest request, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.Reject(staffId, request.Reason, Now()), cancellationToken);

    public Task<StaffClaimDetailResponse> PayAsync(
        Guid staffId, Guid claimId, PayClaimRequest request, CancellationToken cancellationToken = default) =>
        ActAsync(claimId, claim => claim.Pay(staffId, request.PaymentReference, Now()), cancellationToken);

    /// <summary>Load → run one domain action → save → return the updated claim.</summary>
    private async Task<StaffClaimDetailResponse> ActAsync(Guid claimId, Action<Claim> action, CancellationToken cancellationToken)
    {
        var claim = await LoadAsync(claimId, cancellationToken);
        action(claim);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDetailAsync(claim, cancellationToken);
    }

    private async Task<Claim> LoadAsync(Guid claimId, CancellationToken cancellationToken) =>
        await claims.FindByIdAsync(claimId, cancellationToken)
        ?? throw new DomainException(DomainErrors.NotFound, "Claim not found.");

    private async Task<StaffClaimDetailResponse> ToDetailAsync(Claim claim, CancellationToken cancellationToken)
    {
        var claimant = await users.FindByIdAsync(claim.ClaimantId, cancellationToken);
        var policy = await policies.FindByIdAsync(claim.PolicyId, cancellationToken);
        return StaffClaimDetailResponse.From(claim, claimant, policy);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
