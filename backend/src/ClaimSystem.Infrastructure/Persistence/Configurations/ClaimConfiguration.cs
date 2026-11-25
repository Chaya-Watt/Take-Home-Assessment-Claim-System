using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence.Configurations;

internal sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    /// <summary>Shadow property (column only, not on the domain class) used as the concurrency token.</summary>
    public const string Version = "Version";

    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasIndex(c => c.ClaimNumber).IsUnique();
        builder.HasIndex(c => c.Status); // staff queue filters by status

        builder.Property<Guid>(Version).IsConcurrencyToken();

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.ClaimantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.AssignedStaffId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Policy>().WithMany().HasForeignKey(c => c.PolicyId).OnDelete(DeleteBehavior.Restrict);

        // Child rows belong to the aggregate; EF fills the private _history / _informationRequests fields.
        builder.HasMany(c => c.History).WithOne().HasForeignKey(h => h.ClaimId);
        builder.HasMany(c => c.InformationRequests).WithOne().HasForeignKey(r => r.ClaimId);
    }
}
