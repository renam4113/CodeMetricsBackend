namespace CodeMetrics.Application.DTOs.Commits;

public sealed class CommitStatsDto
{
    public int AddedLines { get; init; }
    public int DeletedLines { get; init; }
    public int ChangedFiles { get; init; }
    public int TotalChanges => AddedLines + DeletedLines;
}

public sealed class CommitDetailDto
{
    public string Hash { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string RepoName { get; init; } = string.Empty;
    public string CommitterEmail { get; init; } = string.Empty;
    public string CommitterName { get; init; } = string.Empty;
    public CommitStatsDto Stats { get; init; } = new();
}

public sealed class CommitListItemDto
{
    public string Hash { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string RepoName { get; init; } = string.Empty;
    public string CommitterEmail { get; init; } = string.Empty;
    public string CommitterName { get; init; } = string.Empty;
}
