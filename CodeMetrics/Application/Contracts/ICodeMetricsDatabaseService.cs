using CodeMetrics.Application.DTOs.Commits;
using CodeMetrics.Application.DTOs.Database;

namespace CodeMetrics.Application.Contracts;

public interface ICodeMetricsDatabaseService
{
    Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync();
    Task<RepositoryDto?> GetRepositoryAsync(string repoName);
    Task<IReadOnlyList<BranchDto>> GetBranchesByRepoAsync(string repoName);
    Task<IReadOnlyList<CommitListItemDto>> GetCommitsByRepoAsync(string repoName);
    Task<CommitListItemDto?> GetCommitByHashAsync(string hash);
    Task<CommitStatsRecordDto?> GetCommitStatsByHashAsync(string commitHash);
    Task<UserDto?> GetUserByEmailAsync(string email);
}
