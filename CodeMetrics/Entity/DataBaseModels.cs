using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CodeMetrics.Entity
{
    public class Repository
    {
        public string RepoName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsFork { get; set; }
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
        public DateTimeOffset CreatedAt { get; set; }

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
        public int TotalChanges { get; set; }
        public string CommitHash { get; set; } = null!;

    }

    public class User
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
