using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimSystem.Infrastructure.Persistence;

/// <summary>One row per year holding the last claim number issued that year. Infrastructure-only table.</summary>
internal sealed class ClaimNumberCounter
{
    public int Year { get; set; }
    public long LastValue { get; set; }
}

internal sealed class ClaimNumberCounterConfiguration : IEntityTypeConfiguration<ClaimNumberCounter>
{
    public void Configure(EntityTypeBuilder<ClaimNumberCounter> builder)
    {
        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).ValueGeneratedNever();
    }
}
