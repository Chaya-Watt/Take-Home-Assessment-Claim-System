using ClaimSystem.Domain.Claims;
using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence.Configurations;

internal sealed class InformationRequestConfiguration : IEntityTypeConfiguration<InformationRequest>
{
    public void Configure(EntityTypeBuilder<InformationRequest> builder)
    {
        builder.ToTable("InformationRequests");
        builder.Property(r => r.Id).ValueGeneratedNever(); // see ClaimStatusHistoryConfiguration

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RequestedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
