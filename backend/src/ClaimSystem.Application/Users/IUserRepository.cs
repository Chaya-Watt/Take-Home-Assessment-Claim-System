using ClaimSystem.Domain.Users;

namespace ClaimSystem.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    /// <summary>Everyone who can work on claims (Staff and Supervisor).</summary>
    Task<IReadOnlyList<User>> ListStaffAsync(CancellationToken cancellationToken = default);

    void Add(User user);
}
