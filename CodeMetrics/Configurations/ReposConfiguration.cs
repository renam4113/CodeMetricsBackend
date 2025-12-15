using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.HasKey(r => r.RepoName);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(r => r.ProjectKey)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasIndex(r => r.ProjectKey);
        builder.HasIndex(r => r.RepoName);
    }
}