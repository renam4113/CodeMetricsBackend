using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitStatsConfiguration : IEntityTypeConfiguration<CommitStats>
{
    public void Configure(EntityTypeBuilder<CommitStats> builder)
    {
        builder.HasKey(cs => cs.CommitHash);

        builder.HasIndex(cs => cs.CommitHash).IsUnique();
    }
}