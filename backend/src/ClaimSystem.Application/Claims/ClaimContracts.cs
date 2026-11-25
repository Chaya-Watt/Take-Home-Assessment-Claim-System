using ClaimSystem.Domain.Claims;

namespace ClaimSystem.Application.Claims;

// Request/response shapes of the claimant claim endpoints. Amounts are in satang (1 THB = 100).

public sealed record SubmitClaimRequest(
    Guid PolicyId,
    IncidentType IncidentType,
    DateOnly IncidentDate,
    string Location,
    string Description,
    long ClaimedAmount);

public sealed record InformationResponseRequest(string Response);

public sealed record ClaimSummaryResponse(
    Guid Id,
    string ClaimNumber,
    Guid PolicyId,
    IncidentType IncidentType,
    DateOnly IncidentDate,
    long ClaimedAmount,
    ClaimStatus Status,
    DateTime SubmittedAt)
{
    public static ClaimSummaryResponse From(Claim claim) => new(
        claim.Id, claim.ClaimNumber, claim.PolicyId, claim.IncidentType, claim.IncidentDate,
        claim.ClaimedAmount, claim.Status, claim.SubmittedAt);
}

/// <summary>What the claimant sees: their claim, the decision, the timeline. Internal assessment notes are not included.</summary>
public sealed record ClaimDetailResponse(
    Guid Id,
    string ClaimNumber,
    Guid PolicyId,
    string PolicyNumber,
    IncidentType IncidentType,
    DateOnly IncidentDate,
    string Location,
    string Description,
    long ClaimedAmount,
    ClaimStatus Status,
    DateTime SubmittedAt,
    long? ApprovedAmount,
    string? DecisionReason,
    DateTime? DecidedAt,
    string? PaymentReference,
    DateTime? PaidAt,
    IReadOnlyList<TimelineEntryResponse> Timeline,
    IReadOnlyList<InformationRequestResponse> InformationRequests)
{
    public static ClaimDetailResponse From(Claim claim, string policyNumber) => new(
        claim.Id, claim.ClaimNumber, claim.PolicyId, policyNumber, claim.IncidentType, claim.IncidentDate,
        claim.Location, claim.Description, claim.ClaimedAmount, claim.Status, claim.SubmittedAt,
        claim.ApprovedAmount, claim.DecisionReason, claim.DecidedAt, claim.PaymentReference, claim.PaidAt,
        [.. claim.History.OrderBy(h => h.ChangedAt).Select(h => new TimelineEntryResponse(h.FromStatus, h.ToStatus, h.ChangedAt, h.Note))],
        [.. claim.InformationRequests.OrderBy(r => r.RequestedAt).Select(InformationRequestResponse.From)]);
}

public sealed record TimelineEntryResponse(ClaimStatus? FromStatus, ClaimStatus ToStatus, DateTime ChangedAt, string? Note);

public sealed record InformationRequestResponse(
    Guid Id,
    string Message,
    DateOnly? DueDate,
    DateTime RequestedAt,
    string? ResponseText,
    DateTime? RespondedAt,
    bool IsOpen)
{
    public static InformationRequestResponse From(InformationRequest r) =>
        new(r.Id, r.Message, r.DueDate, r.RequestedAt, r.ResponseText, r.RespondedAt, r.IsOpen);
}
