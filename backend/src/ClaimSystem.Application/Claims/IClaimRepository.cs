using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;

namespace ClaimSystem.Application.Claims;

public interface IClaimRepository
{
    /// <summary>Loads the whole aggregate (history and information requests).</summary>
    Task<Claim?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Newest first. Children are not loaded.</summary>
    Task<PagedResult<Claim>> ListByClaimantAsync(
        Guid claimantId, ClaimStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Newest first. <paramref name="queue"/> null = all claims.</summary>
    Task<PagedResult<Claim>> ListForStaffAsync(
        Guid staffId, StaffClaimQueue? queue, ClaimStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Number of claims in each status (statuses with no claims are left out).</summary>
    Task<IReadOnlyDictionary<ClaimStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>All claims that are not final, with the time they entered their current status.</summary>
    Task<IReadOnlyList<OpenClaimSnapshot>> ListOpenAsync(CancellationToken cancellationToken = default);

    void Add(Claim claim);
}

/// <summary>A lightweight read of an open claim for the supervisor dashboard.</summary>
public sealed record OpenClaimSnapshot(
    Guid Id,
    string ClaimNumber,
    ClaimStatus Status,
    Guid? AssignedStaffId,
    DateTime SubmittedAt,
    DateTime InStatusSince);
