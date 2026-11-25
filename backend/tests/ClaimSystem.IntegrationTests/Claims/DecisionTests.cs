using System.Net;
using System.Net.Http.Json;
using ClaimSystem.Application.Claims;
using ClaimSystem.Domain.Claims;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests.Claims;

/// <summary>Approve / reject / pay over HTTP, and what the claimant sees afterwards.</summary>
[Collection(ApiCollection.Name)]
public class DecisionTests(WebApplicationFactory<Program> factory)
{
    private const string Claimant1 = "claimant1@example.com";
    private const string Staff1 = "staff1@example.com";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- Happy paths

    [Fact]
    public async Task ApproveThenPay_ClaimantSeesApprovedAmountAndPayment()
    {
        var (claimant, staff, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);

        var approved = await OkAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claimId}/approve", new ApproveClaimRequest(30_000_00, "Deductible 5,000 THB applied")));
        Assert.Equal(ClaimStatus.Approved, approved.Status);
        Assert.NotNull(approved.DecidedAt);

        var paid = await OkAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claimId}/pay", new PayClaimRequest("TRF-20261125-0001")));
        Assert.Equal(ClaimStatus.Paid, paid.Status);

        var seen = await claimant.GetFromJsonAsync<ClaimDetailResponse>($"/api/claims/{claimId}", TestApi.Json, CancellationToken);
        Assert.Equal(ClaimStatus.Paid, seen!.Status);
        Assert.Equal(30_000_00, seen.ApprovedAmount);
        Assert.Equal("Deductible 5,000 THB applied", seen.DecisionReason);
        Assert.Equal("TRF-20261125-0001", seen.PaymentReference);
        Assert.NotNull(seen.PaidAt);
        Assert.Equal(
            [ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.Assessed, ClaimStatus.Approved, ClaimStatus.Paid],
            seen.Timeline.Select(t => t.ToStatus));
    }

    [Fact]
    public async Task Reject_ClaimantSeesReason_AndNothingElseCanHappen()
    {
        var (claimant, staff, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);

        var rejected = await OkAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claimId}/reject", new RejectClaimRequest("Damage happened before the policy started")));
        Assert.Equal(ClaimStatus.Rejected, rejected.Status);

        var seen = await claimant.GetFromJsonAsync<ClaimDetailResponse>($"/api/claims/{claimId}", TestApi.Json, CancellationToken);
        Assert.Equal(ClaimStatus.Rejected, seen!.Status);
        Assert.Equal("Damage happened before the policy started", seen.DecisionReason);
        Assert.Null(seen.ApprovedAmount);

        // Rejected is final.
        var approve = await staff.PostJsonAsync($"/api/staff/claims/{claimId}/approve", new ApproveClaimRequest(10_000_00, null));
        Assert.Equal(HttpStatusCode.Conflict, approve.StatusCode);
    }

    // ---------------------------------------------------------------- Rules

    [Fact]
    public async Task Approve_MoreThanAssessed_Returns400()
    {
        var (_, staff, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);

        var response = await staff.PostJsonAsync(
            $"/api/staff/claims/{claimId}/approve", new ApproveClaimRequest(35_000_01, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithoutReason_Returns400()
    {
        var (_, staff, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);

        var response = await staff.PostJsonAsync($"/api/staff/claims/{claimId}/reject", new RejectClaimRequest(" "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_BeforeApproval_Returns409()
    {
        var (_, staff, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);

        var response = await staff.PostJsonAsync($"/api/staff/claims/{claimId}/pay", new PayClaimRequest("TRF-1"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Approve_ByStaffWhoIsNotAssigned_Returns403()
    {
        var (_, _, claimId) = await AssessedClaimAsync(claimed: 40_000_00, assessed: 35_000_00);
        var otherStaff = await factory.CreateClientForAsync("staff2@example.com");

        var response = await otherStaff.PostJsonAsync(
            $"/api/staff/claims/{claimId}/approve", new ApproveClaimRequest(10_000_00, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- Helpers

    /// <summary>claimant1 submits, staff1 picks it up and assesses it.</summary>
    private async Task<(HttpClient Claimant, HttpClient Staff, Guid ClaimId)> AssessedClaimAsync(long claimed, long assessed)
    {
        var claimant = await factory.CreateClientForAsync(Claimant1);
        var staff = await factory.CreateClientForAsync(Staff1);
        var claim = await claimant.SubmitClaimAsync(claimedAmount: claimed);

        await OkAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync($"/api/staff/claims/{claim.Id}/assign"));
        await OkAsync<StaffClaimDetailResponse>(await staff.PostJsonAsync(
            $"/api/staff/claims/{claim.Id}/assessment", new AssessClaimRequest(assessed, "Repair quote checked")));

        return (claimant, staff, claim.Id);
    }

    private static async Task<T> OkAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(CancellationToken)}");
        return await response.ReadAsync<T>();
    }
}
