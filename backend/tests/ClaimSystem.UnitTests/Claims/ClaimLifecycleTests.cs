using ClaimSystem.Domain.Claims;
using static ClaimSystem.UnitTests.Claims.ClaimFixtures;

namespace ClaimSystem.UnitTests.Claims;

/// <summary>Valid paths through the state machine.</summary>
public class ClaimLifecycleTests
{
    [Fact]
    public void HappyPath_SubmitToPaid_RecordsEveryStepInHistory()
    {
        var claim = Paid();

        Assert.Equal(ClaimStatus.Paid, claim.Status);
        Assert.Equal(80_000_00, claim.AssessedAmount);
        Assert.Equal(80_000_00, claim.ApprovedAmount);
        Assert.Equal("PAY-0001", claim.PaymentReference);
        Assert.Equal(Now, claim.PaidAt);
        Assert.Equal(
            [ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.Assessed, ClaimStatus.Approved, ClaimStatus.Paid],
            claim.History.Select(h => h.ToStatus));
    }

    [Fact]
    public void Assign_SetsStaffAndMovesToUnderReview()
    {
        var claim = UnderReview();

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(StaffId, claim.AssignedStaffId);
        Assert.Equal(Now, claim.AssignedAt);
    }

    [Fact]
    public void RequestInformation_ThenClaimantResponds_ReturnsToUnderReview()
    {
        var claim = UnderReview();

        var request = claim.RequestInformation(StaffId, "Please upload the police report", Today.AddDays(7), Now);
        Assert.Equal(ClaimStatus.PendingInformation, claim.Status);
        Assert.True(request.IsOpen);

        claim.ProvideInformation(ClaimantId, request.Id, "Uploaded, see attachment", Now.AddHours(2));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.False(request.IsOpen);
        Assert.Equal("Uploaded, see attachment", request.ResponseText);
        Assert.Equal(Now.AddHours(2), request.RespondedAt);
    }

    [Fact]
    public void RequestInformation_CanHappenMoreThanOnce()
    {
        var claim = UnderReview();
        var first = claim.RequestInformation(StaffId, "Police report", null, Now);
        claim.ProvideInformation(ClaimantId, first.Id, "Done", Now);

        var second = claim.RequestInformation(StaffId, "Repair quote", null, Now);
        claim.ProvideInformation(ClaimantId, second.Id, "Done", Now);

        Assert.Equal(2, claim.InformationRequests.Count);
        Assert.All(claim.InformationRequests, r => Assert.False(r.IsOpen));
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    [Fact]
    public void Approve_WithLowerAmountThanAssessed_StoresApprovedAmountAndNote()
    {
        var claim = Assessed();

        claim.Approve(StaffId, 50_000_00, "Deductible applied", Now);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(50_000_00, claim.ApprovedAmount);
        Assert.Equal("Deductible applied", claim.DecisionReason);
        Assert.Equal(Now, claim.DecidedAt);
    }

    [Fact]
    public void Reject_FromUnderReview_StoresReason()
    {
        var claim = Rejected();

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal("Not covered", claim.DecisionReason);
        Assert.Equal(Now, claim.DecidedAt);
        Assert.Equal("Not covered", claim.History[^1].Note);
    }

    [Fact]
    public void Reject_FromAssessed_IsAllowed()
    {
        var claim = Assessed();

        claim.Reject(StaffId, "Fraud suspected", Now);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
    }

    [Fact]
    public void Withdraw_BeforeAssignment_MovesToWithdrawn()
    {
        var claim = Withdrawn();

        Assert.Equal(ClaimStatus.Withdrawn, claim.Status);
        Assert.Equal(ClaimantId, claim.History[^1].ChangedBy);
    }
}
