using ClaimSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // All Ids are Guid v7 created in the domain, never by the database.
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
