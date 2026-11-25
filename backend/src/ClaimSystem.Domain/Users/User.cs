namespace ClaimSystem.Domain.Users;

public enum UserRole
{
    Claimant,
    Staff,
    Supervisor,
}

public sealed class User
{
    private User() { } // EF Core

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }

    /// <summary>Set by the auth layer (Phase 3) using PasswordHasher.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public static User Create(string email, string fullName, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            Role = role,
        };
    }

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
