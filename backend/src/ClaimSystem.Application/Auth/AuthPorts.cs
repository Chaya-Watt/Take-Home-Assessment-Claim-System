using ClaimSystem.Domain.Users;

namespace ClaimSystem.Application.Auth;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Claim names inside the JWT. Shared by the token issuer and the API that reads them.</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Role = "role";
    public const string Name = "name";
    public const string Email = "email";
}
