namespace ClaimSystem.Domain.Policies;

public enum ProductType
{
    Motor,
    Health,
    Property,
}

/// <summary>An insurance policy held by a claimant (seeded data — no policy management in scope).</summary>
public sealed class Policy
{
    private Policy() { } // EF Core

    public Guid Id { get; private set; }
    public string PolicyNumber { get; private set; } = null!;

    /// <summary>User.Id of the policy holder.</summary>
    public Guid HolderId { get; private set; }

    public ProductType ProductType { get; private set; }

    /// <summary>Maximum payable amount, in satang.</summary>
    public long SumInsured { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    public static Policy Create(
        string policyNumber,
        Guid holderId,
        ProductType productType,
        long sumInsured,
        DateOnly startDate,
        DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sumInsured);
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        }

        return new Policy
        {
            Id = Guid.CreateVersion7(),
            PolicyNumber = policyNumber,
            HolderId = holderId,
            ProductType = productType,
            SumInsured = sumInsured,
            StartDate = startDate,
            EndDate = endDate,
        };
    }

    public bool Covers(DateOnly date) => date >= StartDate && date <= EndDate;
}
