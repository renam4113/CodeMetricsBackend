using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(r => r.BranchName);

        builder.HasOne<Repository>()
            .WithMany()
            .HasForeignKey(b => b.repoName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Commit>()
            .WithOne() 
            .HasForeignKey<Branch>(b => b.lastCommitHash) 
            .HasPrincipalKey<Commit>(c => c.Hash) 
            .OnDelete(DeleteBehavior.Restrict);

    }
}
