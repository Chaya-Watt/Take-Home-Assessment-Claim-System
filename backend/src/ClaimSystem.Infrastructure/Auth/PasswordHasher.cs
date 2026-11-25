using ClaimSystem.Application.Auth;
using ClaimSystem.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace ClaimSystem.Infrastructure.Auth;

/// <summary>ASP.NET Core Identity's hasher (PBKDF2 + salt), without the rest of Identity.</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
