using System.Net;
using System.Net.Http.Json;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Supervisor;
using ClaimSystem.Domain.Claims;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests.Supervisor;

/// <summary>
/// The dashboard runs on the shared dev database, so tests compare before/after values
/// instead of expecting exact totals (integration tests in the collection run one at a time).
/// </summary>
[Collection(ApiCollection.Name)]
public class SupervisorDashboardTests(WebApplicationFactory<Program> factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dashboard_FollowsAClaimFromSubmittedToAssignedToRejected()
    {
        var supervisor = await factory.CreateClientForAsync("supervisor@example.com");
        var claimant = await factory.CreateClientForAsync("claimant1@example.com");
        var staff2 = await factory.CreateClientForAsync("staff2@example.com");
        var before = await DashboardAsync(supervisor);

        // Submitted: one more claim waiting to be picked up.
        var claim = await claimant.SubmitClaimAsync();
        var submitted = await DashboardAsync(supervisor);
        Assert.Equal(before.ClaimsByStatus[ClaimStatus.Submitted] + 1, submitted.ClaimsByStatus[ClaimStatus.Submitted]);
        Assert.Equal(before.UnassignedCount + 1, submitted.UnassignedCount);

        // Picked up by staff2: shows in staff2's workload.
        (await staff2.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign")).EnsureSuccessStatusCode();
        var assigned = await DashboardAsync(supervisor);
        Assert.Equal(before.UnassignedCount, assigned.UnassignedCount);
        Assert.Equal(before.ClaimsByStatus[ClaimStatus.UnderReview] + 1, assigned.ClaimsByStatus[ClaimStatus.UnderReview]);

        var pim = assigned.Staff.Single(s => s.FullName == "Pim Srisuk");
        Assert.Equal(Workload(before, "Pim Srisuk") + 1, pim.OpenClaims);
        var item = Assert.Single(pim.Claims, c => c.Id == claim.Id);
        Assert.Equal(ClaimStatus.UnderReview, item.Status);
        Assert.Equal("Pim Srisuk", item.AssignedStaffName);
        Assert.Equal(DateTimeKind.Utc, item.InStatusSince.Kind);

        // Rejected: final, so it leaves the workload.
        (await staff2.PostJsonAsync($"/api/staff/claims/{claim.Id}/reject", new RejectClaimRequest("Not covered"))).EnsureSuccessStatusCode();
        var rejected = await DashboardAsync(supervisor);
        Assert.Equal(before.ClaimsByStatus[ClaimStatus.Rejected] + 1, rejected.ClaimsByStatus[ClaimStatus.Rejected]);
        Assert.Equal(Workload(before, "Pim Srisuk"), Workload(rejected, "Pim Srisuk"));
        Assert.DoesNotContain(rejected.Staff.SelectMany(s => s.Claims), c => c.Id == claim.Id);
    }

    [Fact]
    public async Task Dashboard_ListsOnlyOpenClaims_OldestFirst()
    {
        var supervisor = await factory.CreateClientForAsync("supervisor@example.com");
        await (await factory.CreateClientForAsync("claimant1@example.com")).SubmitClaimAsync(); // at least one open claim

        var dashboard = await DashboardAsync(supervisor);

        Assert.All(Enum.GetValues<ClaimStatus>(), status => Assert.True(dashboard.ClaimsByStatus.ContainsKey(status)));
        Assert.NotEmpty(dashboard.OldestUnassigned);
        Assert.All(dashboard.OldestUnassigned, c => Assert.Equal(ClaimStatus.Submitted, c.Status));
        Assert.Equal(dashboard.OldestUnassigned.OrderBy(c => c.SubmittedAt), dashboard.OldestUnassigned);

        Assert.All(dashboard.LongestInCurrentStatus, c => Assert.False(c.Status.IsFinal()));
        Assert.Equal(dashboard.LongestInCurrentStatus.OrderBy(c => c.InStatusSince), dashboard.LongestInCurrentStatus);
        Assert.All(dashboard.Staff, s => Assert.Equal(s.Claims.Count, s.OpenClaims));
    }

    [Theory]
    [InlineData("staff1@example.com")]
    [InlineData("claimant1@example.com")]
    public async Task Dashboard_ForNonSupervisor_Returns403(string email)
    {
        var client = await factory.CreateClientForAsync(email);

        var response = await client.GetAsync("/api/supervisor/dashboard", CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<SupervisorDashboardResponse> DashboardAsync(HttpClient supervisor) =>
        (await supervisor.GetFromJsonAsync<SupervisorDashboardResponse>("/api/supervisor/dashboard", TestApi.Json, CancellationToken))!;

    private static int Workload(SupervisorDashboardResponse dashboard, string fullName) =>
        dashboard.Staff.Single(s => s.FullName == fullName).OpenClaims;
}
