using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;

namespace ClaimSystem.Application.Claims;

// Request/response shapes of the staff claim endpoints. Amounts are in satang (1 THB = 100).

/// <summary>Which claims the staff list shows. No queue = all claims.</summary>
public enum StaffClaimQueue
{
    /// <summary>Submitted and waiting for someone to pick it up.</summary>
    Unassigned,

    /// <summary>Assigned to the calling staff member.</summary>
    Mine,
}

public sealed record RequestInformationRequest(string Message, DateOnly? DueDate);

public sealed record AssessClaimRequest(long AssessedAmount, string Summary);

public sealed record StaffClaimSummaryResponse(
    Guid Id,
    string ClaimNumber,
    IncidentType IncidentType,
    DateOnly IncidentDate,
    long ClaimedAmount,
    ClaimStatus Status,
    DateTime SubmittedAt,
    Guid? AssignedStaffId,
    DateTime? AssignedAt)
{
    public static StaffClaimSummaryResponse From(Claim claim) => new(
        claim.Id, claim.ClaimNumber, claim.IncidentType, claim.IncidentDate, claim.ClaimedAmount,
        claim.Status, claim.SubmittedAt, claim.AssignedStaffId, claim.AssignedAt);
}

public sealed record ClaimantInfo(Guid Id, string FullName, string Email);

public sealed record PolicyInfo(Guid Id, string PolicyNumber, ProductType ProductType, long SumInsured);

/// <summary>Everything staff need to work on a claim, including the internal assessment.</summary>
public sealed record StaffClaimDetailResponse(
    Guid Id,
    string ClaimNumber,
    ClaimStatus Status,
    ClaimantInfo Claimant,
    PolicyInfo Policy,
    IncidentType IncidentType,
    DateOnly IncidentDate,
    string Location,
    string Description,
    long ClaimedAmount,
    DateTime SubmittedAt,
    Guid? AssignedStaffId,
    DateTime? AssignedAt,
    long? AssessedAmount,
    string? AssessmentSummary,
    long? ApprovedAmount,
    string? DecisionReason,
    DateTime? DecidedAt,
    string? PaymentReference,
    DateTime? PaidAt,
    IReadOnlyList<TimelineEntryResponse> Timeline,
    IReadOnlyList<InformationRequestResponse> InformationRequests)
{
    public static StaffClaimDetailResponse From(Claim claim, User? claimant, Policy? policy) => new(
        claim.Id, claim.ClaimNumber, claim.Status,
        new ClaimantInfo(claim.ClaimantId, claimant?.FullName ?? string.Empty, claimant?.Email ?? string.Empty),
        new PolicyInfo(claim.PolicyId, policy?.PolicyNumber ?? string.Empty, policy?.ProductType ?? default, policy?.SumInsured ?? 0),
        claim.IncidentType, claim.IncidentDate, claim.Location, claim.Description, claim.ClaimedAmount, claim.SubmittedAt,
        claim.AssignedStaffId, claim.AssignedAt, claim.AssessedAmount, claim.AssessmentSummary,
        claim.ApprovedAmount, claim.DecisionReason, claim.DecidedAt, claim.PaymentReference, claim.PaidAt,
        [.. claim.History.OrderBy(h => h.ChangedAt).Select(h => new TimelineEntryResponse(h.FromStatus, h.ToStatus, h.ChangedAt, h.Note))],
        [.. claim.InformationRequests.OrderBy(r => r.RequestedAt).Select(InformationRequestResponse.From)]);
}
