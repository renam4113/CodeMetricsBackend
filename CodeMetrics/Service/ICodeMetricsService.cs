using CodeMetrics.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Service
{
    public interface ICodeMetricsService
    {
        Task<ContentResult> UpdateDataBase(string? projectKey, string? reposName, string? branch, int limit);
        Task<ContentResult> GetCommit(string projectKey, string reposKey, string hash);
        Task<ContentResult> GetCommits(string projectKey, string reposKey);
        Task<ContentResult> GetProjectCommitsByPeriod(string name, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<ContentResult> GetRepoCommitsByPeriod(string name, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<ContentResult> GetMetricByProject(string name, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<object> GetAuthorSummaryAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<List<string>> GetReposNameByProject(string projectKey);
        Task<object> GetAuthorPerformanceAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate);
    }
}
