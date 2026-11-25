using System.Security.Claims;
using ClaimSystem.Application.Auth;

namespace ClaimSystem.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's Id (JWT "sub"). Only call on endpoints that require authorization.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(AuthClaims.UserId)
            ?? throw new InvalidOperationException("The request is not authenticated."));
}
