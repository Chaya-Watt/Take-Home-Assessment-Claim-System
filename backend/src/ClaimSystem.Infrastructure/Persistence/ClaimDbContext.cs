using ClaimSystem.Application.Common;
using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;
using ClaimSystem.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Infrastructure.Persistence;

/// <summary>Also the Application's <see cref="IUnitOfWork"/>: DbContext.SaveChangesAsync already matches it.</summary>
public sealed class ClaimDbContext(DbContextOptions<ClaimDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Claim> Claims => Set<Claim>();
    internal DbSet<ClaimNumberCounter> ClaimNumberCounters => Set<ClaimNumberCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums as text: readable in the DB and safe if enum members are reordered.
        configurationBuilder.Properties<Enum>().HaveConversion<string>();

        // SQLite stores DateTime as TEXT and reads it back as Unspecified; all our times are UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpClaimVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpClaimVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Every saved claim gets a new <c>Version</c>. EF puts the version it loaded into the UPDATE's WHERE clause,
    /// so if someone else saved the claim in between, no row matches and EF throws DbUpdateConcurrencyException.
    /// </summary>
    private void BumpClaimVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Claim>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(ClaimConfiguration.Version).CurrentValue = Guid.NewGuid();
            }
        }
    }
}
