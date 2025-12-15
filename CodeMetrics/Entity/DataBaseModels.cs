using CodeMetricsApi.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CodeMetrics.Entity
{
    public class Project
    {
        public string ProjectKey { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Repository
    {
        public string RepoName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsFork { get; set; }
        public string ProjectKey { get; set; }
    }

    public class Branch
    {
        public string BranchName { get; set; } = string.Empty;
        public string repoName { get; set; } = string.Empty;
        public string lastCommitHash { get; set; } = string.Empty;
    }

    public class Commit
    {
        [Key]
        [Column(TypeName = "varchar(64)")]
        public string Hash { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string repoName { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "text")]
        public string authorEmail { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string authorName { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string committerEmail { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string committerName { get; set; } = string.Empty;
    }



    public class CommitStats
    {
        public int AddedLines { get; set; }
        public int DeletedLines { get; set; }
        public int ChangedFiles { get; set; }
        public int TotalChanges => AddedLines + DeletedLines;
        public string CommitHash { get; set; }

    }

    public class User
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
