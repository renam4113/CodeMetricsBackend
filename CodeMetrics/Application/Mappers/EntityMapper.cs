using CodeMetrics.Application.DTOs.Commits;
using CodeMetrics.Application.DTOs.Database;
using CodeMetrics.Entity;

namespace CodeMetrics.Application.Mappers;

public static class EntityMapper
{
    public static RepositoryDto ToDto(Repository entity) => new()
    {
        RepoName = entity.RepoName,
        OwnerName = entity.OwnerName,
        DefaultBranch = entity.DefaultBranch,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        IsFork = entity.IsFork
    };

    public static BranchDto ToDto(Branch entity) => new()
    {
        BranchName = entity.BranchName,
        RepoName = entity.repoName,
        LastCommitHash = entity.lastCommitHash
    };

    public static UserDto ToDto(User entity) => new()
    {
        UserEmail = entity.UserEmail,
        UserName = entity.UserName
    };

    public static CommitStatsRecordDto ToDto(CommitStats entity) => new()
    {
        CommitHash = entity.CommitHash,
        AddedLines = entity.AddedLines,
        DeletedLines = entity.DeletedLines,
        ChangedFiles = entity.ChangedFiles,
        TotalChanges = entity.TotalChanges
    };

    public static CommitListItemDto ToListItem(Commit entity) => new()
    {
        Hash = entity.Hash,
        Message = entity.Message,
        AuthorEmail = entity.authorEmail,
        AuthorName = entity.authorName,
        CreatedAt = entity.CreatedAt,
        RepoName = entity.repoName,
        CommitterEmail = entity.committerEmail,
        CommitterName = entity.committerName
    };

    public static CommitDetailDto ToDetail(Commit entity, CommitStats? stats) => new()
    {
        Hash = entity.Hash,
        Message = entity.Message,
        AuthorEmail = entity.authorEmail,
        AuthorName = entity.authorName,
        CreatedAt = entity.CreatedAt,
        RepoName = entity.repoName,
        CommitterEmail = entity.committerEmail,
        CommitterName = entity.committerName,
        Stats = stats is null
            ? new CommitStatsDto()
            : new CommitStatsDto
            {
                AddedLines = stats.AddedLines,
                DeletedLines = stats.DeletedLines,
                ChangedFiles = stats.ChangedFiles
            }
    };
}
