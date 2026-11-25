using ClaimSystem.Domain.Common;

namespace ClaimSystem.Domain.Claims;

/// <summary>Staff asks the claimant for more information; the claimant answers once.</summary>
public sealed class InformationRequest
{
    private InformationRequest() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public string Message { get; private set; } = null!;
    public DateOnly? DueDate { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string? ResponseText { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public bool IsOpen => RespondedAt is null;

    internal static InformationRequest Create(Guid claimId, Guid requestedBy, string message, DateOnly? dueDate, DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClaimId = claimId,
            RequestedBy = requestedBy,
            Message = message.Trim(),
            DueDate = dueDate,
            RequestedAt = now,
        };

    internal void Respond(string responseText, DateTime now)
    {
        if (!IsOpen)
        {
            throw new DomainException(DomainErrors.InvalidTransition, "This information request has already been answered.");
        }

        ResponseText = responseText.Trim();
        RespondedAt = now;
    }
}
