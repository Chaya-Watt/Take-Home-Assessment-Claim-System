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
