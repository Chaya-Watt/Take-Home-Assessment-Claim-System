using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimSystem.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations, then seeds demo users and policies if the database is empty.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        // Passwords are added in Phase 3 (auth).
        var claimant1 = User.Create("claimant1@example.com", "Somchai Jaidee", UserRole.Claimant);
        var claimant2 = User.Create("claimant2@example.com", "Suda Rakdee", UserRole.Claimant);
        db.Users.AddRange(
            claimant1,
            claimant2,
            User.Create("staff1@example.com", "Anan Wongsa", UserRole.Staff),
            User.Create("staff2@example.com", "Pim Srisuk", UserRole.Staff),
            User.Create("supervisor@example.com", "Wichai Thongdee", UserRole.Supervisor));

        // Policies run from 1 Jan this year to 31 Dec next year, so demo incidents "today" are always covered.
        var start = new DateOnly(now.Year, 1, 1);
        var end = new DateOnly(now.Year + 1, 12, 31);
        db.Policies.AddRange(
            Policy.Create("POL-MTR-0001", claimant1.Id, ProductType.Motor, 500_000_00, start, end),
            Policy.Create("POL-PRP-0001", claimant1.Id, ProductType.Property, 3_000_000_00, start, end),
            Policy.Create("POL-HLT-0001", claimant2.Id, ProductType.Health, 1_000_000_00, start, end));

        await db.SaveChangesAsync(cancellationToken);
    }
}
