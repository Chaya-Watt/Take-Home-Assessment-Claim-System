using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Infrastructure.Persistence.Repositories;

internal sealed class ClaimRepository(ClaimDbContext db) : IClaimRepository
{
    public Task<Claim?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims
            .Include(c => c.History)
            .Include(c => c.InformationRequests)
            .AsSplitQuery() // one query per child table instead of one wide JOIN
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Claim>> ListByClaimantAsync(
        Guid claimantId, ClaimStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Claims.AsNoTracking().Where(c => c.ClaimantId == claimantId);
        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.SubmittedAt)
            .ThenByDescending(c => c.ClaimNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, page, pageSize, total);
    }

    public void Add(Claim claim) => db.Claims.Add(claim);
}
