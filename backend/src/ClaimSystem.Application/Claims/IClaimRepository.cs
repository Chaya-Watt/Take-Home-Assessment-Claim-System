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

    void Add(Claim claim);
}
