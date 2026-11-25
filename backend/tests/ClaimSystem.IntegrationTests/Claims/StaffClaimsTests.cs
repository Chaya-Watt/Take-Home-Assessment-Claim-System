using System.Net;
using System.Net.Http.Json;
using ClaimSystem.Application.Claims;
using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests.Claims;

/// <summary>Staff intake and assessment over HTTP. Each test works on claims it submits itself.</summary>
[Collection(ApiCollection.Name)]
public class StaffClaimsTests(WebApplicationFactory<Program> factory)
{
    private const string Claimant1 = "claimant1@example.com";
    private const string Staff1 = "staff1@example.com";
    private const string Staff2 = "staff2@example.com";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static DateOnly Today => BusinessCalendar.Today(DateTime.UtcNow);

    // ---------------------------------------------------------------- Happy path

    [Fact]
    public async Task AssignRequestInformationRespondAssess_FullFlow()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var staff = await factory.CreateClientForAsync(Staff1);
        var claim = await claimant.SubmitClaimAsync(claimedAmount: 40_000_00);

        // Staff picks it up.
        var assigned = await ReadAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign"));
        Assert.Equal(ClaimStatus.UnderReview, assigned.Status);
        Assert.NotNull(assigned.AssignedStaffId);
        Assert.Equal("Somchai Jaidee", assigned.Claimant.FullName);
        Assert.Equal("POL-MTR-0001", assigned.Policy.PolicyNumber);

        // Staff asks for more information.
        var pending = await ReadAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/information-requests",
            new RequestInformationRequest("Please send the police report number", Today.AddDays(7))));
        Assert.Equal(ClaimStatus.PendingInformation, pending.Status);
        var request = Assert.Single(pending.InformationRequests);

        // The claimant sees the open request and answers it.
        var seenByClaimant = await claimant.GetFromJsonAsync<ClaimDetailResponse>($"/api/claims/{claim.Id}", TestApi.Json, CancellationToken);
        Assert.True(Assert.Single(seenByClaimant!.InformationRequests).IsOpen);

        var answered = await ReadAsync<ClaimDetailResponse>(await claimant.PostJsonAsync(
            $"/api/claims/{claim.Id}/information-requests/{request.Id}/response",
            new InformationResponseRequest("Police report no. 1234/2569")));
        Assert.Equal(ClaimStatus.UnderReview, answered.Status);
        Assert.Equal("Police report no. 1234/2569", answered.InformationRequests[0].ResponseText);

        // Staff assesses.
        var assessed = await ReadAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/assessment",
            new AssessClaimRequest(35_000_00, "Repair quote checked; bumper and tail light")));
        Assert.Equal(ClaimStatus.Assessed, assessed.Status);
        Assert.Equal(35_000_00, assessed.AssessedAmount);
        Assert.Equal(
            [ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.PendingInformation, ClaimStatus.UnderReview, ClaimStatus.Assessed],
            assessed.Timeline.Select(t => t.ToStatus));
    }

    [Fact]
    public async Task Queues_NewClaimIsUnassigned_ThenMovesToMine()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var staff = await factory.CreateClientForAsync(Staff1);
        var claim = await claimant.SubmitClaimAsync();

        Assert.Contains(claim.Id, await QueueAsync(staff, "Unassigned"));
        Assert.DoesNotContain(claim.Id, await QueueAsync(staff, "Mine"));

        await staff.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign");

        Assert.DoesNotContain(claim.Id, await QueueAsync(staff, "Unassigned"));
        Assert.Contains(claim.Id, await QueueAsync(staff, "Mine"));
    }

    [Fact]
    public async Task Supervisor_CanWorkClaimsLikeStaff()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var supervisor = await factory.CreateClientForAsync("supervisor@example.com");
        var claim = await claimant.SubmitClaimAsync();

        var response = await supervisor.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------------------------------------------------------------- Picking up a claim

    [Fact]
    public async Task Assign_AlreadyAssigned_Returns409()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var claim = await claimant.SubmitClaimAsync();
        await (await factory.CreateClientForAsync(Staff1)).PostJsonAsync($"/api/staff/claims/{claim.Id}/assign");

        var response = await (await factory.CreateClientForAsync(Staff2)).PostJsonAsync($"/api/staff/claims/{claim.Id}/assign");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ByTwoStaffAtTheSameTime_OneWinsTheOtherGets409()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var claim = await claimant.SubmitClaimAsync();
        var staff1 = await factory.CreateClientForAsync(Staff1);
        var staff2 = await factory.CreateClientForAsync(Staff2);

        var responses = await Task.WhenAll(
            staff1.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign"),
            staff2.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign"));

        // Whichever lost gets 409 — from the status check or from the concurrency token.
        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).Order());
    }

    // ---------------------------------------------------------------- Rules

    [Fact]
    public async Task Action_ByStaffWhoIsNotAssigned_Returns403()
    {
        var claim = await AssignedClaimAsync();
        var otherStaff = await factory.CreateClientForAsync(Staff2);

        var response = await otherStaff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/assessment", new AssessClaimRequest(10_000_00, "Looks fine"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assess_MoreThanClaimed_Returns400()
    {
        var claim = await AssignedClaimAsync(claimedAmount: 20_000_00);
        var staff = await factory.CreateClientForAsync(Staff1);

        var response = await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/assessment", new AssessClaimRequest(20_000_01, "Too much"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestInformation_DueDateInThePast_Returns400()
    {
        var claim = await AssignedClaimAsync();
        var staff = await factory.CreateClientForAsync(Staff1);

        var response = await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/information-requests", new RequestInformationRequest("Photos", Today.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RespondToInformationRequest_ByAnotherClaimant_Returns404()
    {
        var claim = await AssignedClaimAsync();
        var staff = await factory.CreateClientForAsync(Staff1);
        var pending = await ReadAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/information-requests", new RequestInformationRequest("Photos", null)));
        var otherClaimant = await factory.CreateClientForAsync("claimant2@example.com");

        var response = await otherClaimant.PostJsonAsync(
            $"/api/claims/{claim.Id}/information-requests/{pending.InformationRequests[0].Id}/response",
            new InformationResponseRequest("Not my claim"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownClaim_Returns404()
    {
        var staff = await factory.CreateClientForAsync(Staff1);

        var response = await staff.GetAsync($"/api/staff/claims/{Guid.NewGuid()}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StaffEndpoints_AsClaimant_Return403()
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);

        var response = await claimant.GetAsync("/api/staff/claims", CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- Helpers

    /// <summary>A new claim submitted by claimant1 and picked up by staff1.</summary>
    private async Task<ClaimDetailResponse> AssignedClaimAsync(long claimedAmount = 20_000_00)
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var claim = await claimant.SubmitClaimAsync(claimedAmount: claimedAmount);
        var assign = await (await factory.CreateClientForAsync(Staff1)).PostJsonAsync($"/api/staff/claims/{claim.Id}/assign");
        assign.EnsureSuccessStatusCode();
        return claim;
    }

    private static async Task<List<Guid>> QueueAsync(HttpClient staff, string queue)
    {
        var page = await staff.GetFromJsonAsync<PagedResult<StaffClaimSummaryResponse>>(
            $"/api/staff/claims?queue={queue}&pageSize=100", TestApi.Json, CancellationToken);
        return [.. page!.Items.Select(c => c.Id)];
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(CancellationToken)}");
        return await response.ReadAsync<T>();
    }
}
