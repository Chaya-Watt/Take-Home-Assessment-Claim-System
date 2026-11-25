namespace ClaimSystem.Domain.Claims;

/// <summary>
/// Submitted → UnderReview ⇄ PendingInformation; UnderReview → Assessed → Approved → Paid.
/// UnderReview/Assessed → Rejected. Submitted → Withdrawn.
/// </summary>
public enum ClaimStatus
{
    Submitted,
    UnderReview,
    PendingInformation,
    Assessed,
    Approved,
    Rejected,
    Paid,
    Withdrawn,
}

public static class ClaimStatusExtensions
{
    /// <summary>Paid, Rejected and Withdrawn are final: nothing more can happen to the claim.</summary>
    public static bool IsFinal(this ClaimStatus status) =>
        status is ClaimStatus.Paid or ClaimStatus.Rejected or ClaimStatus.Withdrawn;
}
