using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;
using static ClaimSystem.UnitTests.Claims.ClaimFixtures;

namespace ClaimSystem.UnitTests.Claims;

public class IncidentTypesAndPolicyTests
{
    [Theory]
    [InlineData(ProductType.Motor, IncidentType.Collision, true)]
    [InlineData(ProductType.Motor, IncidentType.Flood, true)]
    [InlineData(ProductType.Motor, IncidentType.Fire, false)]
    [InlineData(ProductType.Health, IncidentType.Hospitalization, true)]
    [InlineData(ProductType.Health, IncidentType.Theft, false)]
    [InlineData(ProductType.Property, IncidentType.Flood, true)]
    [InlineData(ProductType.Property, IncidentType.Glass, false)]
    public void IsAllowedFor_MatchesProductMapping(ProductType product, IncidentType incident, bool expected) =>
        Assert.Equal(expected, IncidentTypes.IsAllowedFor(product, incident));

    [Fact]
    public void EveryIncidentType_IsCoveredByAtLeastOneProduct()
    {
        var covered = Enum.GetValues<ProductType>().SelectMany(IncidentTypes.AllowedFor).ToHashSet();

        Assert.All(Enum.GetValues<IncidentType>(), type => Assert.Contains(type, covered));
    }

    [Theory]
    [InlineData(2026, 1, 1, true)]    // first day
    [InlineData(2026, 12, 31, true)]  // last day
    [InlineData(2025, 12, 31, false)]
    [InlineData(2027, 1, 1, false)]
    public void Covers_IncludesStartAndEndDates(int year, int month, int day, bool expected) =>
        Assert.Equal(expected, MotorPolicy().Covers(new DateOnly(year, month, day)));
}
