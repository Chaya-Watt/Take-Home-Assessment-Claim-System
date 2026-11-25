namespace ClaimSystem.Domain.Claims;

/// <summary>One row per status change — drives the claimant timeline and staff performance reports.</summary>
public sealed class ClaimStatusHistory
{
    private ClaimStatusHistory() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }

    /// <summary>Null for the first entry (claim submitted).</summary>
    public ClaimStatus? FromStatus { get; private set; }

    public ClaimStatus ToStatus { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Note { get; private set; }

    internal static ClaimStatusHistory Create(
        Guid claimId, ClaimStatus? from, ClaimStatus to, Guid changedBy, DateTime changedAt, string? note) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClaimId = claimId,
            FromStatus = from,
            ToStatus = to,
            ChangedBy = changedBy,
            ChangedAt = changedAt,
            Note = note,
        };
}
