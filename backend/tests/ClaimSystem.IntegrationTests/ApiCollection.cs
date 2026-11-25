using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimSystem.IntegrationTests;

/// <summary>
/// All integration tests share one running API (and therefore one claims.db), and run one class at a time.
/// Separate instances starting in parallel could both try to seed an empty database.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    public const string Name = "Api";
}
