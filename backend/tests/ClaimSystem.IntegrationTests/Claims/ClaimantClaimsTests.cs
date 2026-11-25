using System.Net;
using System.Net.Http.Json;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Application.Policies;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;
using ClaimSystem.Domain.Policies;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests.Claims;

/// <summary>
/// Claimant flow over HTTP. Runs on the shared dev database, so tests only assert on claims they create
/// (claimant1 accumulates claims across runs).
/// </summary>
[Collection(ApiCollection.Name)]
public class ClaimantClaimsTests(WebApplicationFactory<Program> factory)
{
    private const string Claimant1 = "claimant1@example.com";
    private const string Claimant2 = "claimant2@example.com";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateOnly Today => BusinessCalendar.Today(DateTime.UtcNow);

    // ---------------------------------------------------------------- Policies

    [Fact]
    public async Task GetMyPolicies_ReturnsOnlyOwnPoliciesWithCoveredIncidentTypes()
    {
        var client = await factory.CreateClientForAsync(Claimant1);

        var policies = await client.GetFromJsonAsync<List<PolicyResponse>>("/api/policies/mine", TestApi.Json, CancellationToken);

        Assert.NotNull(policies);
        Assert.Equal(["POL-MTR-0001", "POL-PRP-0001"], policies.Select(p => p.PolicyNumber));
        var motor = policies[0];
        Assert.Equal(ProductType.Motor, motor.ProductType);
        Assert.Equal(IncidentTypes.AllowedFor(ProductType.Motor), motor.IncidentTypes);
    }

    // ---------------------------------------------------------------- Submit -> list -> detail

    [Fact]
    public async Task Submit_ThenListAndGet_ShowsTheClaimWithItsTimeline()
    {
        var client = await factory.CreateClientForAsync(Claimant1);
        var policy = await PolicyAsync(client, "POL-MTR-0001");

        var response = await client.PostAsJsonAsync("/api/claims", new SubmitClaimRequest(
            policy.Id, IncidentType.Collision, Today, "Sukhumvit Rd, Bangkok", "Rear-ended at a red light", 45_000_00),
            TestApi.Json, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<ClaimDetailResponse>();
        Assert.Equal($"/api/claims/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Matches($@"^CLM-{Today.Year}-\d{{6}}$", created.ClaimNumber);
        Assert.Equal(ClaimStatus.Submitted, created.Status);

        var mine = await client.GetFromJsonAsync<PagedResult<ClaimSummaryResponse>>("/api/claims/mine", TestApi.Json, CancellationToken);
        Assert.Equal(created.Id, mine!.Items[0].Id); // newest first

        var detail = await client.GetFromJsonAsync<ClaimDetailResponse>($"/api/claims/{created.Id}", TestApi.Json, CancellationToken);
        Assert.Equal("POL-MTR-0001", detail!.PolicyNumber);
        Assert.Equal(45_000_00, detail.ClaimedAmount);
        var entry = Assert.Single(detail.Timeline);
        Assert.Null(entry.FromStatus);
        Assert.Equal(ClaimStatus.Submitted, entry.ToStatus);
    }

    [Fact]
    public async Task Submit_OnAnotherClaimantsPolicy_Returns403()
    {
        var claimant1 = await factory.CreateClientForAsync(Claimant1);
        var someoneElsesPolicy = await PolicyAsync(claimant1, "POL-MTR-0001");
        var claimant2 = await factory.CreateClientForAsync(Claimant2);

        var response = await SubmitAsync(claimant2, someoneElsesPolicy.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_IncidentTypeNotCoveredByPolicy_Returns400()
    {
        var client = await factory.CreateClientForAsync(Claimant1);
        var motor = await PolicyAsync(client, "POL-MTR-0001");

        var response = await SubmitAsync(client, motor.Id, IncidentType.Illness); // a Health incident

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_UnknownPolicy_Returns404()
    {
        var client = await factory.CreateClientForAsync(Claimant1);

        var response = await SubmitAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------- Ownership

    [Fact]
    public async Task AnotherClaimantsClaim_IsNotFound()
    {
        var owner = await factory.CreateClientForAsync(Claimant1);
        var claim = await SubmitMotorClaimAsync(owner);
        var other = await factory.CreateClientForAsync(Claimant2);

        var get = await other.GetAsync($"/api/claims/{claim.Id}", CancellationToken);
        var withdraw = await other.PostAsync($"/api/claims/{claim.Id}/withdraw", null, CancellationToken);
        var list = await other.GetFromJsonAsync<PagedResult<ClaimSummaryResponse>>(
            "/api/claims/mine?pageSize=100", TestApi.Json, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, withdraw.StatusCode);
        Assert.DoesNotContain(list!.Items, c => c.Id == claim.Id);
    }

    // ---------------------------------------------------------------- List filter & paging

    [Fact]
    public async Task ListMine_FiltersByStatusAndPages()
    {
        var client = await factory.CreateClientForAsync(Claimant1);
        var withdrawn = await SubmitMotorClaimAsync(client);
        var submitted = await SubmitMotorClaimAsync(client);
        await client.PostAsync($"/api/claims/{withdrawn.Id}/withdraw", null, CancellationToken);

        var onlyWithdrawn = await client.GetFromJsonAsync<PagedResult<ClaimSummaryResponse>>(
            "/api/claims/mine?status=Withdrawn&pageSize=100", TestApi.Json, CancellationToken);
        var firstPage = await client.GetFromJsonAsync<PagedResult<ClaimSummaryResponse>>(
            "/api/claims/mine?pageSize=1", TestApi.Json, CancellationToken);

        Assert.All(onlyWithdrawn!.Items, c => Assert.Equal(ClaimStatus.Withdrawn, c.Status));
        Assert.Contains(onlyWithdrawn.Items, c => c.Id == withdrawn.Id);
        Assert.DoesNotContain(onlyWithdrawn.Items, c => c.Id == submitted.Id);

        Assert.Single(firstPage!.Items);
        Assert.Equal(1, firstPage.Page);
        Assert.True(firstPage.TotalCount >= 2);
    }

    // ---------------------------------------------------------------- Withdraw

    [Fact]
    public async Task Withdraw_SubmittedClaim_ThenAgain_Returns409()
    {
        var client = await factory.CreateClientForAsync(Claimant1);
        var claim = await SubmitMotorClaimAsync(client);

        var first = await client.PostAsync($"/api/claims/{claim.Id}/withdraw", null, CancellationToken);
        var second = await client.PostAsync($"/api/claims/{claim.Id}/withdraw", null, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await first.ReadAsync<ClaimDetailResponse>();
        Assert.Equal(ClaimStatus.Withdrawn, body.Status);
        Assert.Equal([ClaimStatus.Submitted, ClaimStatus.Withdrawn], body.Timeline.Select(t => t.ToStatus));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ---------------------------------------------------------------- Who may call

    [Theory]
    [InlineData("/api/claims/mine")]
    [InlineData("/api/policies/mine")]
    public async Task ClaimantEndpoints_AsStaff_Return403(string url)
    {
        var staff = await factory.CreateClientForAsync("staff1@example.com");

        var response = await staff.GetAsync(url, CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/claims/mine")]
    [InlineData("/api/policies/mine")]
    public async Task ClaimantEndpoints_WithoutToken_Return401(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- Helpers

    private static async Task<PolicyResponse> PolicyAsync(HttpClient client, string policyNumber)
    {
        var policies = await client.GetFromJsonAsync<List<PolicyResponse>>("/api/policies/mine", TestApi.Json, CancellationToken);
        return policies!.Single(p => p.PolicyNumber == policyNumber);
    }

    private static Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, Guid policyId, IncidentType incidentType = IncidentType.Collision) =>
        client.PostAsJsonAsync("/api/claims", new SubmitClaimRequest(
            policyId, incidentType, Today, "Bangkok", "Rear-ended at a traffic light", 20_000_00),
            TestApi.Json, CancellationToken);

    private static async Task<ClaimDetailResponse> SubmitMotorClaimAsync(HttpClient client)
    {
        var policy = await PolicyAsync(client, "POL-MTR-0001");
        var response = await SubmitAsync(client, policy.Id);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ClaimDetailResponse>();
    }
}
