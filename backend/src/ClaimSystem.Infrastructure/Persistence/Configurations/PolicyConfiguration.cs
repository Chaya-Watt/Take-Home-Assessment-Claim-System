using ClaimSystem.Domain.Policies;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence.Configurations;

internal sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.HasIndex(p => p.PolicyNumber).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.HolderId).OnDelete(DeleteBehavior.Restrict);
    }
}
