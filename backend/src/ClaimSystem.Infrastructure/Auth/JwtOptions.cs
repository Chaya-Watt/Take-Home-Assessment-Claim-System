using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ClaimSystem.Infrastructure.Auth;

/// <summary>Bound from the "Jwt" config section. The signing key must come from config/env, never from code.</summary>
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 secret; at least 32 bytes.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
