using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitConfiguration : IEntityTypeConfiguration<Commit>
{
    public void Configure(EntityTypeBuilder<Commit> builder)
    {
        builder.HasKey(c => c.Hash);

        builder.Property(c => c.Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne<Repository>()
            .WithMany()
            .HasForeignKey(c => c.repoName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CommitStats>()
            .WithOne()
            .HasForeignKey<Commit>(b => b.Hash)
            .HasPrincipalKey<CommitStats>(c => c.CommitHash)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.authorEmail)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.committerEmail)
            .OnDelete(DeleteBehavior.Cascade);
        

        
    }
}