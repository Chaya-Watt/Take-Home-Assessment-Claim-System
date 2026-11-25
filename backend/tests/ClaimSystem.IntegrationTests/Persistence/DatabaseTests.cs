using ClaimSystem.Application.Claims;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;
using ClaimSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.IntegrationTests.Persistence;

/// <summary>
/// Runs against the dev database (src/ClaimSystem.Api/claims.db), which the app migrates and seeds on startup.
/// Tests only add their own new claims, so they can run repeatedly on the same file.
/// </summary>
[Collection(ApiCollection.Name)]
public class DatabaseTests(WebApplicationFactory<Program> factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- Migrations & seed

    [Fact]
    public async Task Startup_AppliesAllMigrations_AndModelMatchesThem()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = Db(scope);

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(CancellationToken));
        Assert.False(db.Database.HasPendingModelChanges(), "Model changed without a migration: run `dotnet ef migrations add`.");
    }

    [Fact]
    public async Task Seed_CreatesDemoUsersAndPolicies()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = Db(scope);

        var users = await db.Users.ToDictionaryAsync(u => u.Email, CancellationToken);
        Assert.Equal(UserRole.Claimant, users["claimant1@example.com"].Role);
        Assert.Equal(UserRole.Claimant, users["claimant2@example.com"].Role);
        Assert.Equal(UserRole.Staff, users["staff1@example.com"].Role);
        Assert.Equal(UserRole.Staff, users["staff2@example.com"].Role);
        Assert.Equal(UserRole.Supervisor, users["supervisor@example.com"].Role);

        // Each claimant holds policies of different product types.
        var policies = await db.Policies.ToListAsync(CancellationToken);
        ProductType[] ProductsOf(string email) =>
            [.. policies.Where(p => p.HolderId == users[email].Id).Select(p => p.ProductType).Order()];
        Assert.Equal([ProductType.Motor, ProductType.Property], ProductsOf("claimant1@example.com"));
        Assert.Equal([ProductType.Health], ProductsOf("claimant2@example.com"));
    }

    [Fact]
    public async Task Seed_RunAgain_DoesNotDuplicateData()
    {
        var before = await CountUsersAsync();

        await factory.Services.InitializeDatabaseAsync(CancellationToken);

        Assert.Equal(before, await CountUsersAsync());
    }

    // ---------------------------------------------------------------- Claim aggregate round trip

    [Fact]
    public async Task Claim_SavedAndReloaded_KeepsChildrenEnumsAndUtcTimes()
    {
        var claimId = await SubmitClaimAsync();
        var staffId = await UserIdAsync("staff1@example.com");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = Db(scope);
            var claim = await LoadAsync(db, claimId);
            claim.Assign(staffId, Now.AddSeconds(1));
            claim.RequestInformation(staffId, "Please upload the police report", null, Now.AddSeconds(2));
            await db.SaveChangesAsync(CancellationToken); // new child rows must be INSERTed, not UPDATEd
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = Db(scope);
            var claim = await LoadAsync(db, claimId);

            Assert.Equal(ClaimStatus.PendingInformation, claim.Status);
            Assert.Equal(
                [ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.PendingInformation],
                claim.History.OrderBy(h => h.ChangedAt).Select(h => h.ToStatus));
            Assert.True(Assert.Single(claim.InformationRequests).IsOpen);
            Assert.Equal(DateTimeKind.Utc, claim.SubmittedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, claim.AssignedAt!.Value.Kind);

            var storedStatus = await db.Database
                .SqlQuery<string>($"SELECT Status AS Value FROM Claims WHERE Id = {claimId}")
                .ToListAsync(CancellationToken);
            Assert.Equal("PendingInformation", Assert.Single(storedStatus));
        }
    }

    [Fact]
    public async Task Claim_SavedConcurrently_SecondSaveThrowsConcurrencyException()
    {
        var claimId = await SubmitClaimAsync();
        var staff1 = await UserIdAsync("staff1@example.com");
        var staff2 = await UserIdAsync("staff2@example.com");

        // Two staff open the same unassigned claim at the same time...
        await using var scopeA = factory.Services.CreateAsyncScope();
        await using var scopeB = factory.Services.CreateAsyncScope();
        var claimA = await LoadAsync(Db(scopeA), claimId);
        var claimB = await LoadAsync(Db(scopeB), claimId);

        // ...both pick it up; the first save wins.
        claimA.Assign(staff1, Now);
        await Db(scopeA).SaveChangesAsync(CancellationToken);

        claimB.Assign(staff2, Now);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Db(scopeB).SaveChangesAsync(CancellationToken));

        await using var verify = factory.Services.CreateAsyncScope();
        Assert.Equal(staff1, (await LoadAsync(Db(verify), claimId)).AssignedStaffId);
    }

    // ---------------------------------------------------------------- Claim numbers

    [Fact]
    public async Task ClaimNumberGenerator_ConcurrentCalls_ReturnUniqueConsecutiveNumbers()
    {
        var numbers = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            return await Generator(scope).NextAsync(Now, CancellationToken);
        }));

        Assert.All(numbers, n => Assert.Matches($@"^CLM-{Now.Year}-\d{{6}}$", n));
        var sequence = numbers.Select(n => int.Parse(n[^6..])).Order().ToArray();
        Assert.Equal(Enumerable.Range(sequence[0], 20), sequence); // no duplicates, no gaps
    }

    [Fact]
    public async Task ClaimNumberGenerator_EachYearHasItsOwnSequence()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var generator = Generator(scope);
        var thisYear = Now;
        var nextYear = Now.AddYears(1);

        var first = await generator.NextAsync(thisYear, CancellationToken);
        var otherYear = await generator.NextAsync(nextYear, CancellationToken);
        var second = await generator.NextAsync(thisYear, CancellationToken);

        Assert.StartsWith($"CLM-{nextYear.Year}-", otherYear);
        Assert.Equal(int.Parse(first[^6..]) + 1, int.Parse(second[^6..])); // unaffected by the other year
    }

    // ---------------------------------------------------------------- Helpers

    private static DateTime Now => DateTime.UtcNow;

    private static ClaimDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<ClaimDbContext>();

    private static IClaimNumberGenerator Generator(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IClaimNumberGenerator>();

    private static Task<Claim> LoadAsync(ClaimDbContext db, Guid claimId) =>
        db.Claims
            .Include(c => c.History)
            .Include(c => c.InformationRequests)
            .SingleAsync(c => c.Id == claimId, CancellationToken);

    private async Task<int> CountUsersAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await Db(scope).Users.CountAsync(CancellationToken);
    }

    private async Task<Guid> UserIdAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await Db(scope).Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync(CancellationToken);
    }

    /// <summary>Submits a claim on the seeded motor policy and returns its Id.</summary>
    private async Task<Guid> SubmitClaimAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = Db(scope);
        var policy = await db.Policies.SingleAsync(p => p.PolicyNumber == "POL-MTR-0001", CancellationToken);
        var number = await Generator(scope).NextAsync(Now, CancellationToken);

        var claim = Claim.Submit(
            number, policy.HolderId, policy, IncidentType.Collision, DateOnly.FromDateTime(Now),
            "Bangkok", "Rear-ended at a traffic light", 50_000_00, Now);
        db.Claims.Add(claim);
        await db.SaveChangesAsync(CancellationToken);

        return claim.Id;
    }
}
