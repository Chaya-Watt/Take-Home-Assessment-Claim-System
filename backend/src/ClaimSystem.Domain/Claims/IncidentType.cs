using ClaimSystem.Domain.Policies;

namespace ClaimSystem.Domain.Claims;

public enum IncidentType
{
    // Motor
    Collision,
    Theft,
    Glass,

    // Health
    Illness,
    Accident,
    Hospitalization,

    // Property
    Fire,
    Burglary,
    WaterDamage,

    // Motor + Property
    Flood,
}

/// <summary>Which incident types can be claimed under each product type.</summary>
public static class IncidentTypes
{
    private static readonly Dictionary<ProductType, IncidentType[]> Allowed = new()
    {
        [ProductType.Motor] = [IncidentType.Collision, IncidentType.Theft, IncidentType.Flood, IncidentType.Glass],
        [ProductType.Health] = [IncidentType.Illness, IncidentType.Accident, IncidentType.Hospitalization],
        [ProductType.Property] = [IncidentType.Fire, IncidentType.Flood, IncidentType.Burglary, IncidentType.WaterDamage],
    };

    public static IReadOnlyList<IncidentType> AllowedFor(ProductType productType) => Allowed[productType];

    public static bool IsAllowedFor(ProductType productType, IncidentType incidentType) =>
        Allowed[productType].Contains(incidentType);
}
