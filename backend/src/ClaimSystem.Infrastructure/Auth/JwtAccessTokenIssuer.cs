using ClaimSystem.Application.Auth;
using ClaimSystem.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ClaimSystem.Infrastructure.Auth;

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(User user)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(jwt.ExpiryMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [AuthClaims.UserId] = user.Id.ToString(),
                [AuthClaims.Role] = user.Role.ToString(),
                [AuthClaims.Name] = user.FullName,
                [AuthClaims.Email] = user.Email,
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
