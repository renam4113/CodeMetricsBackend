using CodeMetrics.Configurations;
using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeMetrics.Context
{
       public class CodeMetricsDbContext : DbContext
        {
            public CodeMetricsDbContext(DbContextOptions<CodeMetricsDbContext> options) : base(options) { }

            public DbSet<Project> Projects { get; set; }
            public DbSet<Repository> Repositories { get; set; }
            public DbSet<Commit> Commits { get; set; }
            public DbSet<CommitStats> CommitStats { get; set; }
            public DbSet<User> Users { get; set; }
            public DbSet<Branch> Branches { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplyConfiguration(new ProjectConfiguration());
                modelBuilder.ApplyConfiguration(new RepositoryConfiguration());
                modelBuilder.ApplyConfiguration(new CommitConfiguration());
                modelBuilder.ApplyConfiguration(new CommitStatsConfiguration());
                modelBuilder.ApplyConfiguration(new UserConfiguration());
                modelBuilder.ApplyConfiguration(new BranchConfiguration());

                base.OnModelCreating(modelBuilder);
            }
        }
}
