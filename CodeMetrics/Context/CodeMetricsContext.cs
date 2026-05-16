using CodeMetrics.Configurations;
using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace CodeMetrics.Context;

public class CodeMetricsDbContext : DbContext
{
    public CodeMetricsDbContext(DbContextOptions<CodeMetricsDbContext> options) : base(options) { }
    public DbSet<Repository> Repositories { get; set; }
    public DbSet<Commit> Commits { get; set; }
    public DbSet<CommitStats> CommitStats { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Branch> Branches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RepositoryConfiguration());
        modelBuilder.ApplyConfiguration(new CommitConfiguration());
        modelBuilder.ApplyConfiguration(new CommitStatsConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());

        var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
        convertToProviderExpression: v => v.ToUniversalTime(),
            convertFromProviderExpression: v => v
);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties()
                            .Where(p => p.PropertyType == typeof(DateTimeOffset)))
            {
                modelBuilder.Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(utcConverter);
            }
        }
        base.OnModelCreating(modelBuilder);
    }
}
