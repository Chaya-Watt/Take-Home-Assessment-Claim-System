using ClaimSystem.Domain.Common;
using ClaimSystem.Domain.Policies;

namespace ClaimSystem.Domain.Claims;

/// <summary>
/// Claim aggregate. All status changes go through the methods below, which enforce the
/// state machine and business rules, and append a <see cref="ClaimStatusHistory"/> entry.
/// Amounts are in satang. Times are UTC.
/// </summary>
public sealed class Claim
{
    private readonly List<ClaimStatusHistory> _history = [];
    private readonly List<InformationRequest> _informationRequests = [];

    private Claim() { } // EF Core

    public Guid Id { get; private set; }
    public string ClaimNumber { get; private set; } = null!;
    public Guid ClaimantId { get; private set; }
    public Guid PolicyId { get; private set; }

    public IncidentType IncidentType { get; private set; }
    public DateOnly IncidentDate { get; private set; }
    public string Location { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public long ClaimedAmount { get; private set; }

    public ClaimStatus Status { get; private set; }
    public Guid? AssignedStaffId { get; private set; }

    public long? AssessedAmount { get; private set; }
    public string? AssessmentSummary { get; private set; }
    public long? ApprovedAmount { get; private set; }
    public string? DecisionReason { get; private set; }
    public string? PaymentReference { get; private set; }

    public DateTime SubmittedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public IReadOnlyList<ClaimStatusHistory> History => _history.AsReadOnly();
    public IReadOnlyList<InformationRequest> InformationRequests => _informationRequests.AsReadOnly();

    // ---------------------------------------------------------------- Claimant actions

    public static Claim Submit(
        string claimNumber,
        Guid claimantId,
        Policy policy,
        IncidentType incidentType,
        DateOnly incidentDate,
        string location,
        string description,
        long claimedAmount,
        DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimNumber);
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.HolderId != claimantId)
        {
            throw new DomainException(DomainErrors.NotAllowed, "You can only claim against your own policy.");
        }

        if (incidentDate > BusinessCalendar.Today(now))
        {
            throw Invalid("Incident date cannot be in the future.");
        }

        if (!policy.Covers(incidentDate))
        {
            throw Invalid($"Policy {policy.PolicyNumber} does not cover incidents on {incidentDate:yyyy-MM-dd}.");
        }

        if (!IncidentTypes.IsAllowedFor(policy.ProductType, incidentType))
        {
            throw Invalid($"Incident type {incidentType} is not covered by a {policy.ProductType} policy.");
        }

        if (claimedAmount <= 0 || claimedAmount > policy.SumInsured)
        {
            throw Invalid("Claimed amount must be greater than zero and not exceed the sum insured.");
        }

        RequireText(location, "Location");
        RequireText(description, "Description");

        var claim = new Claim
        {
            Id = Guid.CreateVersion7(),
            ClaimNumber = claimNumber,
            ClaimantId = claimantId,
            PolicyId = policy.Id,
            IncidentType = incidentType,
            IncidentDate = incidentDate,
            Location = location.Trim(),
            Description = description.Trim(),
            ClaimedAmount = claimedAmount,
            Status = ClaimStatus.Submitted,
            SubmittedAt = now,
        };
        claim._history.Add(ClaimStatusHistory.Create(claim.Id, null, ClaimStatus.Submitted, claimantId, now, null));
        return claim;
    }

    public void ProvideInformation(Guid claimantId, Guid requestId, string response, DateTime now)
    {
        EnsureStatus("provide information for", ClaimStatus.PendingInformation);
        EnsureOwnedBy(claimantId);
        RequireText(response, "Response");

        var request = _informationRequests.SingleOrDefault(r => r.Id == requestId)
            ?? throw new DomainException(DomainErrors.NotFound, "Information request not found.");

        request.Respond(response, now);
        TransitionTo(ClaimStatus.UnderReview, claimantId, now);
    }

    public void Withdraw(Guid claimantId, DateTime now)
    {
        EnsureStatus("withdraw", ClaimStatus.Submitted);
        EnsureOwnedBy(claimantId);
        TransitionTo(ClaimStatus.Withdrawn, claimantId, now);
    }

    // ---------------------------------------------------------------- Staff actions

    public void Assign(Guid staffId, DateTime now)
    {
        EnsureStatus("assign", ClaimStatus.Submitted);
        AssignedStaffId = staffId;
        AssignedAt = now;
        TransitionTo(ClaimStatus.UnderReview, staffId, now);
    }

    public InformationRequest RequestInformation(Guid staffId, string message, DateOnly? dueDate, DateTime now)
    {
        EnsureStatus("request information for", ClaimStatus.UnderReview);
        EnsureAssignedTo(staffId);
        RequireText(message, "Message");
        if (dueDate < BusinessCalendar.Today(now))
        {
            throw Invalid("Due date cannot be in the past.");
        }

        var request = InformationRequest.Create(Id, staffId, message, dueDate, now);
        _informationRequests.Add(request);
        TransitionTo(ClaimStatus.PendingInformation, staffId, now, message);
        return request;
    }

    public void Assess(Guid staffId, long assessedAmount, string summary, DateTime now)
    {
        EnsureStatus("assess", ClaimStatus.UnderReview);
        EnsureAssignedTo(staffId);
        RequireText(summary, "Assessment summary");
        if (assessedAmount <= 0 || assessedAmount > ClaimedAmount)
        {
            throw Invalid("Assessed amount must be greater than zero and not exceed the claimed amount.");
        }

        AssessedAmount = assessedAmount;
        AssessmentSummary = summary.Trim();
        TransitionTo(ClaimStatus.Assessed, staffId, now);
    }

    public void Approve(Guid staffId, long approvedAmount, string? note, DateTime now)
    {
        EnsureStatus("approve", ClaimStatus.Assessed);
        EnsureAssignedTo(staffId);
        if (approvedAmount <= 0 || approvedAmount > AssessedAmount)
        {
            throw Invalid("Approved amount must be greater than zero and not exceed the assessed amount.");
        }

        ApprovedAmount = approvedAmount;
        DecisionReason = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        DecidedAt = now;
        TransitionTo(ClaimStatus.Approved, staffId, now, DecisionReason);
    }

    public void Reject(Guid staffId, string reason, DateTime now)
    {
        EnsureStatus("reject", ClaimStatus.UnderReview, ClaimStatus.Assessed);
        EnsureAssignedTo(staffId);
        RequireText(reason, "Rejection reason");

        DecisionReason = reason.Trim();
        DecidedAt = now;
        TransitionTo(ClaimStatus.Rejected, staffId, now, DecisionReason);
    }

    public void Pay(Guid staffId, string paymentReference, DateTime now)
    {
        EnsureStatus("pay", ClaimStatus.Approved);
        EnsureAssignedTo(staffId);
        RequireText(paymentReference, "Payment reference");

        PaymentReference = paymentReference.Trim();
        PaidAt = now;
        TransitionTo(ClaimStatus.Paid, staffId, now);
    }

    // ---------------------------------------------------------------- Helpers

    private void TransitionTo(ClaimStatus next, Guid changedBy, DateTime now, string? note = null)
    {
        _history.Add(ClaimStatusHistory.Create(Id, Status, next, changedBy, now, note));
        Status = next;
    }

    private void EnsureStatus(string action, params ClaimStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new DomainException(DomainErrors.InvalidTransition, $"Cannot {action} a claim that is {Status}.");
        }
    }

    private void EnsureOwnedBy(Guid claimantId)
    {
        if (ClaimantId != claimantId)
        {
            throw new DomainException(DomainErrors.NotAllowed, "This claim belongs to another claimant.");
        }
    }

    private void EnsureAssignedTo(Guid staffId)
    {
        if (AssignedStaffId != staffId)
        {
            throw new DomainException(DomainErrors.NotAllowed, "Only the staff member assigned to this claim can do this.");
        }
    }

    private static void RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"{field} is required.");
        }
    }

    private static DomainException Invalid(string message) => new(DomainErrors.Validation, message);
}
