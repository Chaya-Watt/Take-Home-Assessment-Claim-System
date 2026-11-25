using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaimSystem.Application.Auth;
using ClaimSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests;

/// <summary>Helpers shared by the HTTP tests.</summary>
internal static class TestApi
{
    /// <summary>Same JSON shape as the API (camelCase, enums as strings).</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>An HttpClient already signed in as the given (seeded) user.</summary>
    public static async Task<HttpClient> CreateClientForAsync(
        this WebApplicationFactory<Program> factory, string email, string password = DatabaseInitializer.DemoPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), CancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.ReadAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json, CancellationToken)
        ?? throw new InvalidOperationException("Empty response body.");
}
