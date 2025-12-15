using CodeMetrics.Entity;
using CodeMetrics.Models;
using CodeMetricsApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Service
{
    public interface ICodeMetricsService
    {
        Task<ContentResult> UpdateDataBase(string? reposName, string? branch, int limit);
        Task<ContentResult> GetCommit(string reposKey, string hash);
        Task<ContentResult> GetCommits(string reposKey);
        Task<CommitStats?> GetCommitStats(string reposKey, string hash);
        Task<ContentResult> GetRepoCommitsByPeriod(string name, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<ContentResult> GetCommitsByPeriod(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<ContentResult> GetMetric(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<object> GetAuthorSummaryAsync(string authorName, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<List<string>> GetReposName();
        Task<object> GetAuthorPerformanceAsync(string authorName, DateTimeOffset startDate, DateTimeOffset endDate);
    }
}
