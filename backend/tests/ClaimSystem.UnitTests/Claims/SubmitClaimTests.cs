using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Common;
using static ClaimSystem.UnitTests.Claims.ClaimFixtures;

namespace ClaimSystem.UnitTests.Claims;

public class SubmitClaimTests
{
    [Fact]
    public void Submit_ValidInput_CreatesSubmittedClaimWithFirstHistoryEntry()
    {
        var claim = Submitted();

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(ClaimantId, claim.ClaimantId);
        Assert.Equal(100_000_00, claim.ClaimedAmount);
        Assert.Equal(Now, claim.SubmittedAt);
        Assert.Null(claim.AssignedStaffId);

        var entry = Assert.Single(claim.History);
        Assert.Null(entry.FromStatus);
        Assert.Equal(ClaimStatus.Submitted, entry.ToStatus);
        Assert.Equal(ClaimantId, entry.ChangedBy);
    }

    [Fact]
    public void Submit_AgainstSomeoneElsesPolicy_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => Submit(claimantId: OtherClaimantId));
        Assert.Equal(DomainErrors.NotAllowed, ex.Code);
    }

    [Fact]
    public void Submit_IncidentInFuture_Throws() =>
        AssertValidationError(() => Submit(incidentDate: Today.AddDays(1)));

    [Fact]
    public void Submit_IncidentOutsidePolicyPeriod_Throws() =>
        AssertValidationError(() => Submit(incidentDate: new DateOnly(2025, 12, 31)));

    [Fact]
    public void Submit_IncidentTypeNotCoveredByProduct_Throws() =>
        AssertValidationError(() => Submit(incidentType: IncidentType.Illness)); // Health type on a Motor policy

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(500_000_01)] // 1 satang over the sum insured
    public void Submit_ClaimedAmountOutOfRange_Throws(long amount) =>
        AssertValidationError(() => Submit(claimedAmount: amount));

    [Fact]
    public void Submit_ClaimedAmountEqualToSumInsured_IsAllowed()
    {
        var claim = Submit(claimedAmount: 500_000_00);
        Assert.Equal(500_000_00, claim.ClaimedAmount);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("Bangkok", " ")]
    public void Submit_MissingLocationOrDescription_Throws(string location, string description) =>
        AssertValidationError(() => Submit(location: location, description: description));

    private static Claim Submit(
        Guid? claimantId = null,
        IncidentType incidentType = IncidentType.Collision,
        DateOnly? incidentDate = null,
        string location = "Bangkok",
        string description = "Rear-ended",
        long claimedAmount = 100_000_00) =>
        Claim.Submit(
            "CLM-2026-000001", claimantId ?? ClaimantId, MotorPolicy(), incidentType,
            incidentDate ?? Today.AddDays(-1), location, description, claimedAmount, Now);

    private static void AssertValidationError(Action act)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Equal(DomainErrors.Validation, ex.Code);
    }
}
