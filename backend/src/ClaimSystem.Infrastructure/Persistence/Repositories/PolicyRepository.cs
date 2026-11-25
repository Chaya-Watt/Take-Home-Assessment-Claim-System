using ClaimSystem.Application.Policies;
using ClaimSystem.Domain.Policies;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Infrastructure.Persistence.Repositories;

internal sealed class PolicyRepository(ClaimDbContext db) : IPolicyRepository
{
    public Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Policies.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Policy>> ListByHolderAsync(Guid holderId, CancellationToken cancellationToken = default) =>
        await db.Policies.AsNoTracking()
            .Where(p => p.HolderId == holderId)
            .OrderBy(p => p.PolicyNumber)
            .ToListAsync(cancellationToken);
}
