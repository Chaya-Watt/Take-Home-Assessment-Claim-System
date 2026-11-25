using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaimSystem.Application.Auth;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Policies;
using ClaimSystem.Domain.Common;
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

    /// <summary>Submits a claim dated today on one of the signed-in claimant's policies.</summary>
    public static async Task<ClaimDetailResponse> SubmitClaimAsync(
        this HttpClient claimant, string policyNumber = "POL-MTR-0001", long claimedAmount = 20_000_00)
    {
        var policies = await claimant.GetFromJsonAsync<List<PolicyResponse>>("/api/policies/mine", Json, CancellationToken);
        var policy = policies!.Single(p => p.PolicyNumber == policyNumber);

        var response = await claimant.PostJsonAsync("/api/claims", new SubmitClaimRequest(
            policy.Id, policy.IncidentTypes[0], BusinessCalendar.Today(DateTime.UtcNow),
            "Bangkok", "Test incident", claimedAmount));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ClaimDetailResponse>();
    }

    /// <summary>POST with an optional JSON body, using the API's JSON options.</summary>
    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object? body = null) =>
        body is null
            ? client.PostAsync(url, null, CancellationToken)
            : client.PostAsJsonAsync(url, body, Json, CancellationToken);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json, CancellationToken)
        ?? throw new InvalidOperationException("Empty response body.");
}
