using System.Net.Mail;
using ClaimSystem.Application.Common;
using ClaimSystem.Application.Users;
using ClaimSystem.Domain.Common;
using ClaimSystem.Domain.Users;

namespace ClaimSystem.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Email, string FullName, UserRole Role);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer)
{
    public const int MinPasswordLength = 8;

    /// <summary>Self-registration always creates a Claimant. Staff accounts are not created through the API.</summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            throw Invalid("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw Invalid("Full name is required.");
        }

        if (request.Password is null || request.Password.Length < MinPasswordLength)
        {
            throw Invalid($"Password must be at least {MinPasswordLength} characters.");
        }

        if (await users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new DomainException(DomainErrors.Conflict, "An account with this email already exists.");
        }

        var user = User.Create(request.Email, request.FullName, UserRole.Claimant);
        user.SetPasswordHash(passwordHasher.Hash(user, request.Password));
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToAuthResponse(user);
    }

    /// <summary>Returns null when the email or password is wrong (the caller answers 401 without saying which).</summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.PasswordHash.Length == 0 || !passwordHasher.Verify(user, request.Password))
        {
            return null;
        }

        return ToAuthResponse(user);
    }

    public async Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : ToUserResponse(user);
    }

    private AuthResponse ToAuthResponse(User user)
    {
        var token = tokenIssuer.Issue(user);
        return new AuthResponse(token.Token, token.ExpiresAt, ToUserResponse(user));
    }

    private static UserResponse ToUserResponse(User user) => new(user.Id, user.Email, user.FullName, user.Role);

    private static DomainException Invalid(string message) => new(DomainErrors.Validation, message);
}
