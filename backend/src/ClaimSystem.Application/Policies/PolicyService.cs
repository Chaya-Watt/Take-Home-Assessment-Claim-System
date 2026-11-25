using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;

namespace ClaimSystem.Application.Policies;

public interface IPolicyRepository
{
    Task<Policy?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Policy>> ListByHolderAsync(Guid holderId, CancellationToken cancellationToken = default);
}

/// <summary>A policy plus the incident types it covers, so the claim form can offer only valid choices.</summary>
public sealed record PolicyResponse(
    Guid Id,
    string PolicyNumber,
    ProductType ProductType,
    long SumInsured,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<IncidentType> IncidentTypes);

public sealed class PolicyService(IPolicyRepository policies)
{
    public async Task<IReadOnlyList<PolicyResponse>> ListMineAsync(Guid holderId, CancellationToken cancellationToken = default)
    {
        var mine = await policies.ListByHolderAsync(holderId, cancellationToken);
        return
        [
            .. mine.Select(p => new PolicyResponse(
                p.Id, p.PolicyNumber, p.ProductType, p.SumInsured, p.StartDate, p.EndDate,
                IncidentTypes.AllowedFor(p.ProductType))),
        ];
    }
}
