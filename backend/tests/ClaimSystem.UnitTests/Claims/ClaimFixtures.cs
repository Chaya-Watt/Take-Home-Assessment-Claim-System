using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;

namespace ClaimSystem.UnitTests.Claims;

/// <summary>Shared test data: a motor policy and helpers that build claims in each status.</summary>
internal static class ClaimFixtures
{
    public static readonly DateTime Now = new(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    public static readonly Guid ClaimantId = Guid.NewGuid();
    public static readonly Guid OtherClaimantId = Guid.NewGuid();
    public static readonly Guid StaffId = Guid.NewGuid();
    public static readonly Guid OtherStaffId = Guid.NewGuid();

    /// <summary>500,000 THB motor policy covering all of 2026.</summary>
    public static Policy MotorPolicy() => Policy.Create(
        "POL-MOTOR-001", ClaimantId, ProductType.Motor, sumInsured: 500_000_00,
        new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    /// <summary>Claims 100,000 THB for a collision yesterday.</summary>
    public static Claim Submitted(long claimedAmount = 100_000_00) => Claim.Submit(
        "CLM-2026-000001", ClaimantId, MotorPolicy(), IncidentType.Collision, Today.AddDays(-1),
        "Bangkok", "Rear-ended at a traffic light", claimedAmount, Now);

    public static Claim UnderReview()
    {
        var claim = Submitted();
        claim.Assign(StaffId, Now);
        return claim;
    }

    public static Claim PendingInformation()
    {
        var claim = UnderReview();
        claim.RequestInformation(StaffId, "Please upload the police report", Today.AddDays(7), Now);
        return claim;
    }

    /// <summary>Assessed at 80,000 THB.</summary>
    public static Claim Assessed()
    {
        var claim = UnderReview();
        claim.Assess(StaffId, 80_000_00, "Repair quote verified", Now);
        return claim;
    }

    public static Claim Approved()
    {
        var claim = Assessed();
        claim.Approve(StaffId, 80_000_00, null, Now);
        return claim;
    }

    public static Claim Paid()
    {
        var claim = Approved();
        claim.Pay(StaffId, "PAY-0001", Now);
        return claim;
    }

    public static Claim Rejected()
    {
        var claim = UnderReview();
        claim.Reject(StaffId, "Not covered", Now);
        return claim;
    }

    public static Claim Withdrawn()
    {
        var claim = Submitted();
        claim.Withdraw(ClaimantId, Now);
        return claim;
    }
}
