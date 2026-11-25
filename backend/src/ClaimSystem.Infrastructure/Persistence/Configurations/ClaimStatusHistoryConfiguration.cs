using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence.Configurations;

internal sealed class ClaimStatusHistoryConfiguration : IEntityTypeConfiguration<ClaimStatusHistory>
{
    public void Configure(EntityTypeBuilder<ClaimStatusHistory> builder)
    {
        builder.ToTable("ClaimStatusHistory");

        // Important for children: with a pre-set key, EF would otherwise treat a newly added
        // history row as an existing one and issue an UPDATE instead of an INSERT.
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.HasOne<User>().WithMany().HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
