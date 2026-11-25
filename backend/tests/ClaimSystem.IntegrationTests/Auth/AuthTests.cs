using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaimSystem.Application.Auth;
using ClaimSystem.Domain.Users;
using ClaimSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthTests(WebApplicationFactory<Program> factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- Register

    [Fact]
    public async Task Register_NewEmail_Returns201WithTokenForClaimant()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test Claimant"), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadAsync<AuthResponse>(response);
        Assert.Equal(email, body.User.Email);
        Assert.Equal(UserRole.Claimant, body.User.Role);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
    }

    [Fact]
    public async Task Register_WithRoleInBody_StillCreatesClaimant()
    {
        // Someone tries to make themselves staff by adding a "role" field.
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = "Password123!", fullName = "Sneaky", role = "Supervisor" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(UserRole.Claimant, (await ReadAsync<AuthResponse>(response)).User.Role);
    }

    [Fact]
    public async Task Register_ExistingEmailInDifferentCase_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("  Claimant1@Example.com ", "Password123!", "Copycat"), CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("conflict", (await ReadAsync<ProblemDetails>(response)).Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData("not-an-email", "Password123!", "Name")]
    [InlineData("", "Password123!", "Name")]
    [InlineData("valid@example.com", "short", "Name")]
    [InlineData("valid@example.com", "Password123!", " ")]
    public async Task Register_InvalidInput_Returns400(string email, string password, string fullName)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, fullName), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ---------------------------------------------------------------- Login & me

    [Theory]
    [InlineData("claimant1@example.com", UserRole.Claimant)]
    [InlineData("staff1@example.com", UserRole.Staff)]
    [InlineData("supervisor@example.com", UserRole.Supervisor)]
    public async Task Login_SeededAccount_ReturnsTokenThatWorksForMe(string email, UserRole role)
    {
        var login = await LoginAsync(email, DatabaseInitializer.DemoPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await ReadAsync<AuthResponse>(login);
        Assert.Equal(role, auth.User.Role);

        var me = await GetMeAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var user = await ReadAsync<UserResponse>(me);
        Assert.Equal(auth.User.Id, user.Id);
        Assert.Equal(role, user.Role);
    }

    [Fact]
    public async Task Login_AfterRegister_Works()
    {
        var email = NewEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "MySecret99", "New User"), CancellationToken);

        var response = await LoginAsync(email.ToUpperInvariant(), "MySecret99");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("claimant1@example.com", "WrongPassword!")]
    [InlineData("nobody@example.com", "Password123!")]
    [InlineData("claimant1@example.com", "")]
    public async Task Login_WrongCredentials_Returns401(string email, string password)
    {
        var response = await LoginAsync(email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var token = (await ReadAsync<AuthResponse>(await LoginAsync("claimant1@example.com", DatabaseInitializer.DemoPassword))).AccessToken;
        var tampered = token[..^4] + (token[^4..] == "AAAA" ? "BBBB" : "AAAA"); // break the signature

        var response = await GetMeAsync(tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- Helpers

    private static string NewEmail() => $"test-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), CancellationToken);

    private Task<HttpResponseMessage> GetMeAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request, CancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json, CancellationToken)
        ?? throw new InvalidOperationException("Empty response body.");
}
