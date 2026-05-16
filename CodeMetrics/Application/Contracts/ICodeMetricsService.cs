using CodeMetrics.Application.Common;
using CodeMetrics.Application.DTOs.Analytics;
using CodeMetrics.Application.DTOs.Commits;

namespace CodeMetrics.Application.Contracts;

public interface ICodeMetricsService
{
    Task<AuthorSummaryDto> GetAuthorSummaryAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<AuthorPerformanceDto> GetAuthorPerformanceAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<ProjectMetricsDto> GetProjectMetricsAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    Task<ServiceResult<IReadOnlyList<CommitListItemDto>>> GetRepoCommitsByPeriodAsync(string repoName, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<ServiceResult<CommitDetailDto>> GetCommitAsync(string repoName, string hash);
    Task<ServiceResult<IReadOnlyList<CommitDetailDto>>> GetCommitsAsync(string repoName);
    Task<ServiceResult<SyncResultDto>> SyncFromGiteaAsync(string? repoName, string? branch, int limit);
    Task<List<string>> GetRepoNamesAsync();
}
