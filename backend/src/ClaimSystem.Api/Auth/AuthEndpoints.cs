using System.Security.Claims;
using ClaimSystem.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaimSystem.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    /// <summary>Creates a Claimant account and signs it in.</summary>
    private static async Task<Created<AuthResponse>> Register(
        RegisterRequest request, AuthService auth, CancellationToken cancellationToken) =>
        TypedResults.Created("/api/auth/me", await auth.RegisterAsync(request, cancellationToken));

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> Login(
        LoginRequest request, AuthService auth, CancellationToken cancellationToken)
    {
        var response = await auth.LoginAsync(request, cancellationToken);
        return response is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid email or password.")
            : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> Me(
        ClaimsPrincipal user, AuthService auth, CancellationToken cancellationToken)
    {
        var response = await auth.GetUserAsync(user.GetUserId(), cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }
}
