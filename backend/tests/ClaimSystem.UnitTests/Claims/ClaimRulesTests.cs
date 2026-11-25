using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;
using static ClaimSystem.UnitTests.Claims.ClaimFixtures;

namespace ClaimSystem.UnitTests.Claims;

/// <summary>Everything the state machine must refuse.</summary>
public class ClaimRulesTests
{
    // Which statuses each action is allowed from. Every other combination must be refused.
    private static readonly Dictionary<string, ClaimStatus[]> AllowedFrom = new()
    {
        ["assign"] = [ClaimStatus.Submitted],
        ["withdraw"] = [ClaimStatus.Submitted],
        ["requestInformation"] = [ClaimStatus.UnderReview],
        ["provideInformation"] = [ClaimStatus.PendingInformation],
        ["assess"] = [ClaimStatus.UnderReview],
        ["approve"] = [ClaimStatus.Assessed],
        ["reject"] = [ClaimStatus.UnderReview, ClaimStatus.Assessed],
        ["pay"] = [ClaimStatus.Approved],
    };

    public static TheoryData<string, ClaimStatus> InvalidTransitions()
    {
        var data = new TheoryData<string, ClaimStatus>();
        foreach (var (action, allowed) in AllowedFrom)
        {
            foreach (var status in Enum.GetValues<ClaimStatus>().Except(allowed))
            {
                data.Add(action, status);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Action_FromDisallowedStatus_ThrowsInvalidTransition(string action, ClaimStatus status)
    {
        var claim = InStatus(status);

        var ex = Assert.Throws<DomainException>(() => Perform(claim, action));

        Assert.Equal(DomainErrors.InvalidTransition, ex.Code);
        Assert.Equal(status, claim.Status); // unchanged
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted, false)]
    [InlineData(ClaimStatus.UnderReview, false)]
    [InlineData(ClaimStatus.PendingInformation, false)]
    [InlineData(ClaimStatus.Assessed, false)]
    [InlineData(ClaimStatus.Approved, false)]
    [InlineData(ClaimStatus.Paid, true)]
    [InlineData(ClaimStatus.Rejected, true)]
    [InlineData(ClaimStatus.Withdrawn, true)]
    public void IsFinal_OnlyForStatusesWithNoFurtherActions(ClaimStatus status, bool expected)
    {
        Assert.Equal(expected, status.IsFinal());

        // Cross-check with the state machine: a final status allows no action at all.
        if (expected)
        {
            Assert.DoesNotContain(AllowedFrom.Values, allowed => allowed.Contains(status));
        }
    }

    [Fact]
    public void Assign_AlreadyAssignedClaim_ThrowsInvalidTransition()
    {
        var claim = UnderReview();

        var ex = Assert.Throws<DomainException>(() => claim.Assign(OtherStaffId, Now));

        Assert.Equal(DomainErrors.InvalidTransition, ex.Code);
        Assert.Equal(StaffId, claim.AssignedStaffId);
    }

    // ---------------------------------------------------------------- Who may act

    [Theory]
    [InlineData("requestInformation", ClaimStatus.UnderReview)]
    [InlineData("assess", ClaimStatus.UnderReview)]
    [InlineData("reject", ClaimStatus.UnderReview)]
    [InlineData("approve", ClaimStatus.Assessed)]
    [InlineData("pay", ClaimStatus.Approved)]
    public void StaffAction_ByStaffNotAssigned_ThrowsNotAllowed(string action, ClaimStatus status)
    {
        var claim = InStatus(status);

        var ex = Assert.Throws<DomainException>(() => Perform(claim, action, staffId: OtherStaffId));

        Assert.Equal(DomainErrors.NotAllowed, ex.Code);
    }

    [Theory]
    [InlineData("withdraw", ClaimStatus.Submitted)]
    [InlineData("provideInformation", ClaimStatus.PendingInformation)]
    public void ClaimantAction_ByAnotherClaimant_ThrowsNotAllowed(string action, ClaimStatus status)
    {
        var claim = InStatus(status);

        var ex = Assert.Throws<DomainException>(() => Perform(claim, action, claimantId: OtherClaimantId));

        Assert.Equal(DomainErrors.NotAllowed, ex.Code);
    }

    // ---------------------------------------------------------------- Information requests

    [Fact]
    public void ProvideInformation_UnknownRequest_ThrowsNotFound()
    {
        var claim = PendingInformation();

        var ex = Assert.Throws<DomainException>(() => claim.ProvideInformation(ClaimantId, Guid.NewGuid(), "x", Now));

        Assert.Equal(DomainErrors.NotFound, ex.Code);
    }

    [Fact]
    public void ProvideInformation_EmptyResponse_ThrowsValidation()
    {
        var claim = PendingInformation();

        var ex = Assert.Throws<DomainException>(() =>
            claim.ProvideInformation(ClaimantId, claim.InformationRequests[0].Id, " ", Now));

        Assert.Equal(DomainErrors.Validation, ex.Code);
    }

    [Fact]
    public void RequestInformation_DueDateInPast_ThrowsValidation()
    {
        var claim = UnderReview();

        var ex = Assert.Throws<DomainException>(() =>
            claim.RequestInformation(StaffId, "Police report", Today.AddDays(-1), Now));

        Assert.Equal(DomainErrors.Validation, ex.Code);
    }

    // ---------------------------------------------------------------- Amounts & required text

    [Theory]
    [InlineData(0)]
    [InlineData(100_000_01)] // more than claimed (100,000 THB)
    public void Assess_AmountOutOfRange_ThrowsValidation(long amount)
    {
        var claim = UnderReview();

        var ex = Assert.Throws<DomainException>(() => claim.Assess(StaffId, amount, "summary", Now));

        Assert.Equal(DomainErrors.Validation, ex.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(80_000_01)] // more than assessed (80,000 THB)
    public void Approve_AmountOutOfRange_ThrowsValidation(long amount)
    {
        var claim = Assessed();

        var ex = Assert.Throws<DomainException>(() => claim.Approve(StaffId, amount, null, Now));

        Assert.Equal(DomainErrors.Validation, ex.Code);
    }

    [Fact]
    public void Reject_WithoutReason_ThrowsValidation()
    {
        var claim = UnderReview();

        var ex = Assert.Throws<DomainException>(() => claim.Reject(StaffId, "", Now));

        Assert.Equal(DomainErrors.Validation, ex.Code);
    }

    // ---------------------------------------------------------------- Helpers

    private static Claim InStatus(ClaimStatus status) => status switch
    {
        ClaimStatus.Submitted => Submitted(),
        ClaimStatus.UnderReview => UnderReview(),
        ClaimStatus.PendingInformation => PendingInformation(),
        ClaimStatus.Assessed => Assessed(),
        ClaimStatus.Approved => Approved(),
        ClaimStatus.Paid => Paid(),
        ClaimStatus.Rejected => Rejected(),
        ClaimStatus.Withdrawn => Withdrawn(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static void Perform(Claim claim, string action, Guid? staffId = null, Guid? claimantId = null)
    {
        var staff = staffId ?? StaffId;
        var claimant = claimantId ?? ClaimantId;
        var openRequestId = claim.InformationRequests.LastOrDefault()?.Id ?? Guid.NewGuid();

        switch (action)
        {
            case "assign": claim.Assign(staff, Now); break;
            case "withdraw": claim.Withdraw(claimant, Now); break;
            case "requestInformation": claim.RequestInformation(staff, "Police report", null, Now); break;
            case "provideInformation": claim.ProvideInformation(claimant, openRequestId, "Done", Now); break;
            case "assess": claim.Assess(staff, 50_000_00, "summary", Now); break;
            case "approve": claim.Approve(staff, 50_000_00, null, Now); break;
            case "reject": claim.Reject(staff, "reason", Now); break;
            case "pay": claim.Pay(staff, "PAY-1", Now); break;
            default: throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}
