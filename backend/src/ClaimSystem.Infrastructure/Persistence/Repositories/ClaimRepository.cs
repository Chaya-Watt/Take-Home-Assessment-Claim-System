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

    public Task<PagedResult<Claim>> ListByClaimantAsync(
        Guid claimantId, ClaimStatus? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        PageAsync(Filter(db.Claims.Where(c => c.ClaimantId == claimantId), status), page, pageSize, cancellationToken);

    public Task<PagedResult<Claim>> ListForStaffAsync(
        Guid staffId, StaffClaimQueue? queue, ClaimStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = queue switch
        {
            StaffClaimQueue.Unassigned => db.Claims.Where(c => c.Status == ClaimStatus.Submitted),
            StaffClaimQueue.Mine => db.Claims.Where(c => c.AssignedStaffId == staffId),
            _ => db.Claims,
        };

        return PageAsync(Filter(query, status), page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<ClaimStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default) =>
        await db.Claims
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

    public async Task<IReadOnlyList<OpenClaimSnapshot>> ListOpenAsync(CancellationToken cancellationToken = default) =>
        await db.Claims
            .Where(c => c.Status != ClaimStatus.Paid && c.Status != ClaimStatus.Rejected && c.Status != ClaimStatus.Withdrawn)
            .Select(c => new OpenClaimSnapshot(
                c.Id, c.ClaimNumber, c.Status, c.AssignedStaffId, c.SubmittedAt,
                c.History.Max(h => h.ChangedAt))) // every claim has at least the "Submitted" entry
            .ToListAsync(cancellationToken);

    public void Add(Claim claim) => db.Claims.Add(claim);

    private static IQueryable<Claim> Filter(IQueryable<Claim> query, ClaimStatus? status) =>
        status is null ? query : query.Where(c => c.Status == status);

    /// <summary>Newest first, without children (lists don't need them).</summary>
    private static async Task<PagedResult<Claim>> PageAsync(
        IQueryable<Claim> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        query = query.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.SubmittedAt)
            .ThenByDescending(c => c.ClaimNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, page, pageSize, total);
    }
}
