namespace CodeMetrics.Application.DTOs.Database;

public sealed class RepositoryDto
{
    public string RepoName { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string DefaultBranch { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool IsFork { get; init; }
}

public sealed class BranchDto
{
    public string BranchName { get; init; } = string.Empty;
    public string RepoName { get; init; } = string.Empty;
    public string LastCommitHash { get; init; } = string.Empty;
}

public sealed class UserDto
{
    public string UserEmail { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

public sealed class CommitStatsRecordDto
{
    public string CommitHash { get; init; } = string.Empty;
    public int AddedLines { get; init; }
    public int DeletedLines { get; init; }
    public int ChangedFiles { get; init; }
    public int TotalChanges { get; init; }
}
