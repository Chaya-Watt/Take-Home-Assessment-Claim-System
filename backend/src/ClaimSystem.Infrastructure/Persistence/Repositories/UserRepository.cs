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

    public void Add(User user) => db.Users.Add(user);
}
