using ClaimSystem.Application.Users;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ClaimDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);
        return db.Users.SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListStaffAsync(CancellationToken cancellationToken = default) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Supervisor)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

    public void Add(User user) => db.Users.Add(user);
}
