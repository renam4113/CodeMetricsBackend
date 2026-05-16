using System.Globalization;
using CodeMetrics.Application.Common;
using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Analytics;
using CodeMetrics.Application.DTOs.Commits;
using CodeMetrics.Application.Mappers;
using CodeMetrics.Clients;
using CodeMetrics.Context;
using CodeMetrics.Entity;
using CodeMetricsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeMetrics.Services;

public sealed class CodeMetricsService : ICodeMetricsService
{
    private readonly GiteaClient _giteaClient;
    private readonly CodeMetricsDbContext _dbContext;

    public CodeMetricsService(CodeMetricsDbContext dbContext, GiteaClient giteaClient)
    {
        _dbContext = dbContext;
        _giteaClient = giteaClient;
    }

    public async Task<AuthorSummaryDto> GetAuthorSummaryAsync(
        string authorEmail,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var (startUtc, endUtc) = NormalizeRange(startDate, endDate);

        var commits = await _dbContext.Commits
            .AsNoTracking()
            .Where(c => c.authorEmail == authorEmail)
            .Where(c => c.CreatedAt >= startUtc && c.CreatedAt <= endUtc)
            .ToListAsync();

        var commitHashes = commits.Select(c => c.Hash).ToList();
        var stats = commitHashes.Count == 0
            ? []
            : await _dbContext.CommitStats
                .AsNoTracking()
                .Where(s => commitHashes.Contains(s.CommitHash))
                .ToListAsync();

        var weekly = commits
            .GroupBy(c => new
            {
                c.CreatedAt.Year,
                Week = ISOWeek.GetWeekOfYear(c.CreatedAt.LocalDateTime)
            })
            .Select(g =>
            {
                var startOfWeek = ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday);
                return new WeeklyActivityDto
                {
                    Year = g.Key.Year,
                    Week = g.Key.Week,
                    From = startOfWeek.ToString("yyyy-MM-dd"),
                    To = startOfWeek.AddDays(6).ToString("yyyy-MM-dd"),
                    CommitCount = g.Count()
                };
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Week)
            .ToList();

        return new AuthorSummaryDto
        {
            Author = authorEmail,
            Period = new PeriodDto
            {
                Start = startUtc.ToString("yyyy-MM-dd"),
                End = endUtc.ToString("yyyy-MM-dd")
            },
            Weekly = weekly,
            AverageChangeSize = stats.Count > 0 ? stats.Average(s => s.TotalChanges) : 0,
            BestDays = commits
                .GroupBy(c => c.CreatedAt.DayOfWeek)
                .Select(g => new DayActivityDto { Day = g.Key.ToString(), Commits = g.Count() })
                .OrderByDescending(g => g.Commits)
                .ToList(),
            BestHours = commits
                .GroupBy(c => c.CreatedAt.Hour)
                .Select(g => new HourActivityDto { Hour = g.Key, Commits = g.Count() })
                .OrderByDescending(g => g.Commits)
                .ToList()
        };
    }

    public async Task<AuthorPerformanceDto> GetAuthorPerformanceAsync(
        string authorEmail,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var (startUtc, endUtc) = NormalizeRange(startDate, endDate);
        var emailLower = authorEmail.ToLowerInvariant();

        var now = DateTime.UtcNow;
        var monthAgo = now.AddDays(-30);
        var fiveWeeksAgo = now.AddDays(-35);

        var commitsCountLastMonth = await _dbContext.Commits
            .AsNoTracking()
            .CountAsync(c => c.authorEmail.ToLower() == emailLower
                             && c.CreatedAt >= monthAgo
                             && c.CreatedAt <= now);

        var lastMonthHashes = await _dbContext.Commits
            .AsNoTracking()
            .Where(c => c.authorEmail.ToLower() == emailLower
                        && c.CreatedAt >= monthAgo
                        && c.CreatedAt <= now)
            .Select(c => c.Hash)
            .ToListAsync();

        var usefulLinesLastMonth = lastMonthHashes.Count == 0
            ? 0
            : await _dbContext.CommitStats
                .AsNoTracking()
                .Where(s => lastMonthHashes.Contains(s.CommitHash))
                .SumAsync(s => s.TotalChanges);

        var fiveWeeksCommits = await _dbContext.Commits
            .AsNoTracking()
            .Where(c => c.authorEmail.ToLower() == emailLower
                        && c.CreatedAt >= fiveWeeksAgo
                        && c.CreatedAt <= now)
            .Select(c => c.CreatedAt)
            .ToListAsync();

        var stability = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var startOfWeek = fiveWeeksAgo.AddDays(7 * i);
                var endOfWeek = startOfWeek.AddDays(7);
                return fiveWeeksCommits.Count(c => c >= startOfWeek && c < endOfWeek);
            })
            .ToList();

        var companyCommitsQuery = _dbContext.Commits
            .AsNoTracking()
            .Where(c => c.CreatedAt >= startUtc && c.CreatedAt <= endUtc);

        var commitsPerAuthor = await companyCommitsQuery
            .GroupBy(c => c.authorEmail)
            .Select(g => new { Author = g.Key, Count = g.Count() })
            .ToListAsync();

        var companyCommitHashes = await companyCommitsQuery
            .Select(c => new { c.authorEmail, c.Hash })
            .ToListAsync();

        var hashes = companyCommitHashes.Select(c => c.Hash).Distinct().ToList();
        var statsByHash = hashes.Count == 0
            ? new Dictionary<string, CommitStats>()
            : await _dbContext.CommitStats
                .AsNoTracking()
                .Where(s => hashes.Contains(s.CommitHash))
                .ToDictionaryAsync(s => s.CommitHash);

        var linesByAuthor = companyCommitHashes
            .GroupBy(c => c.authorEmail)
            .Select(g => new
            {
                Author = g.Key,
                TotalLines = g.Sum(x => statsByHash.TryGetValue(x.Hash, out var s) ? s.TotalChanges : 0)
            })
            .ToList();

        return new AuthorPerformanceDto
        {
            Author = authorEmail,
            Speed = new SpeedMetricsDto
            {
                Commits = commitsCountLastMonth,
                UsefulLines = usefulLinesLastMonth
            },
            Stability = new StabilityMetricsDto { Weeks = stability },
            Normalization = new NormalizationMetricsDto
            {
                MaxCommits = (int)Math.Round(CalculatePercentile(commitsPerAuthor.Select(x => x.Count).ToList(), 95)),
                MinCommits = (int)Math.Round(CalculatePercentile(commitsPerAuthor.Select(x => x.Count).ToList(), 5)),
                MaxLines = (int)Math.Round(CalculatePercentile(linesByAuthor.Select(x => x.TotalLines).ToList(), 95)),
                MinLines = (int)Math.Round(CalculatePercentile(linesByAuthor.Select(x => x.TotalLines).ToList(), 5))
            }
        };
    }

    public async Task<ProjectMetricsDto> GetProjectMetricsAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var (startUtc, endUtc) = NormalizeRange(startDate, endDate);

        var repositoryCount = await _dbContext.Repositories.AsNoTracking().CountAsync();
        var repoNames = await _dbContext.Repositories.AsNoTracking().Select(r => r.RepoName).ToListAsync();

        var commits = await _dbContext.Commits
            .AsNoTracking()
            .Where(c => repoNames.Contains(c.repoName))
            .Where(c => c.CreatedAt >= startUtc && c.CreatedAt <= endUtc)
            .ToListAsync();

        var commitHashes = commits.Select(c => c.Hash).ToList();
        var commitStats = commitHashes.Count == 0
            ? []
            : await _dbContext.CommitStats
                .AsNoTracking()
                .Where(s => commitHashes.Contains(s.CommitHash))
                .ToListAsync();

        var statsByHash = commitStats.ToDictionary(s => s.CommitHash);

        var activeCommitters = commits
            .GroupBy(c => new { c.authorEmail, c.authorName })
            .Select(g => new ActiveCommitterDto
            {
                Name = g.Key.authorName,
                Email = g.Key.authorEmail,
                CommitCount = g.Count(),
                TotalChangedLines = g.Sum(commit =>
                    statsByHash.TryGetValue(commit.Hash, out var stat)
                        ? stat.AddedLines + stat.DeletedLines
                        : 0)
            })
            .OrderByDescending(a => a.CommitCount)
            .ToList();

        return new ProjectMetricsDto
        {
            Project = "CodeMetrics",
            Period = new PeriodDto
            {
                Start = startDate.ToString("yyyy-MM-dd"),
                End = endDate.ToString("yyyy-MM-dd")
            },
            ActiveCommitters = activeCommitters,
            TotalCommits = commits.Count,
            TotalChangedLines = commitStats.Sum(s => s.AddedLines + s.DeletedLines),
            RepositoryCount = repositoryCount
        };
    }

    public async Task<ServiceResult<IReadOnlyList<CommitListItemDto>>> GetRepoCommitsByPeriodAsync(
        string repoName,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var exists = await _dbContext.Repositories.AsNoTracking().AnyAsync(r => r.RepoName == repoName);
        if (!exists)
            return ServiceResult<IReadOnlyList<CommitListItemDto>>.Fail("Repository not found", Application.Common.StatusCodes.Status404NotFound);

        var (startUtc, endUtc) = NormalizeRange(startDate, endDate);

        var items = await _dbContext.Commits
            .AsNoTracking()
            .Where(c => c.repoName == repoName)
            .Where(c => c.CreatedAt >= startUtc && c.CreatedAt <= endUtc)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<CommitListItemDto>>.Ok(items.Select(EntityMapper.ToListItem).ToList());
    }

    public async Task<ServiceResult<CommitDetailDto>> GetCommitAsync(string repoName, string hash)
    {
        var commit = await _dbContext.Commits
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Hash == hash && c.repoName == repoName);

        if (commit is null)
            return ServiceResult<CommitDetailDto>.Fail("Commit not found", Application.Common.StatusCodes.Status404NotFound);

        var stats = await _dbContext.CommitStats
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CommitHash == hash);

        return ServiceResult<CommitDetailDto>.Ok(EntityMapper.ToDetail(commit, stats));
    }

    public async Task<ServiceResult<IReadOnlyList<CommitDetailDto>>> GetCommitsAsync(string repoName)
    {
        try
        {
            var rows = await (
                from c in _dbContext.Commits.AsNoTracking()
                where c.repoName == repoName
                join s in _dbContext.CommitStats.AsNoTracking() on c.Hash equals s.CommitHash into stats
                from s in stats.DefaultIfEmpty()
                orderby c.CreatedAt descending
                select new { c, s }
            ).ToListAsync();

            var result = rows.Select(r => EntityMapper.ToDetail(r.c, r.s)).ToList();
            return ServiceResult<IReadOnlyList<CommitDetailDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            return ServiceResult<IReadOnlyList<CommitDetailDto>>.Fail(ex.Message, Application.Common.StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ServiceResult<SyncResultDto>> SyncFromGiteaAsync(string? repoName, string? branch, int limit)
    {
        _ = repoName;
        _ = limit;

        try
        {
            var repos = await _giteaClient.GetReposAsync();
            if (repos is null || repos.Count == 0)
            {
                return ServiceResult<SyncResultDto>.Ok(new SyncResultDto
                {
                    Message = "No repos to process"
                });
            }

            var repoNames = repos.Select(c => c.Name).ToList();
            var existingRepos = await _dbContext.Repositories
                .Where(r => repoNames.Contains(r.RepoName))
                .Select(r => r.RepoName)
                .ToHashSetAsync();

            var newRepos = repos
                .Where(r => !existingRepos.Contains(r.Name))
                .Select(r => new Repository
                {
                    RepoName = r.Name,
                    OwnerName = "unknown",
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    DefaultBranch = r.DefaultBranch,
                    IsFork = false
                })
                .ToList();

            if (newRepos.Count > 0)
            {
                await _dbContext.Repositories.AddRangeAsync(newRepos);
                await _dbContext.SaveChangesAsync();
            }

            var totalUsers = 0;
            var totalCommits = 0;
            var totalStats = 0;
            var totalBranches = 0;

            foreach (var repo in repos)
            {
                var commitSync = await SyncCommitsAsync(repo.Name);
                if (!commitSync.Success)
                    return commitSync;

                totalUsers += commitSync.Value!.UsersAdded;
                totalCommits += commitSync.Value.CommitsAdded;
                totalStats += commitSync.Value.CommitStatsAdded;

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    var branchSync = await SyncBranchesAsync(repo.Name, branch);
                    if (!branchSync.Success)
                        return ServiceResult<SyncResultDto>.Fail(branchSync.Error!, branchSync.StatusCode);

                    totalBranches += branchSync.Value;
                }
            }

            return ServiceResult<SyncResultDto>.Ok(new SyncResultDto
            {
                Message = $"Sync completed. Added {newRepos.Count} repos.",
                RepositoriesAdded = newRepos.Count,
                UsersAdded = totalUsers,
                CommitsAdded = totalCommits,
                CommitStatsAdded = totalStats,
                BranchesAdded = totalBranches
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<SyncResultDto>.Fail(ex.Message, Application.Common.StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<List<string>> GetRepoNamesAsync()
    {
        var repos = await _giteaClient.GetReposAsync();
        return repos?.Select(c => c.Name).ToList() ?? [];
    }

    private async Task<ServiceResult<SyncResultDto>> SyncCommitsAsync(string repoName)
    {
        try
        {
            var commitsResponse = await _giteaClient.GetCommitsAsync(repoName) ?? [];
            if (commitsResponse.Count == 0)
            {
                return ServiceResult<SyncResultDto>.Ok(new SyncResultDto { Message = "No commits to process" });
            }

            var commitHashes = commitsResponse.Select(c => c.hash).ToList();
            var existingHashes = await _dbContext.Commits
                .Where(c => commitHashes.Contains(c.Hash))
                .Select(c => c.Hash)
                .ToHashSetAsync();

            var existingUsers = await _dbContext.Users.ToDictionaryAsync(u => u.UserEmail, u => u);
            var newUsersDict = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
            var newCommits = new List<Commit>();
            var newStats = new List<CommitStats>();

            foreach (var commitData in commitsResponse)
            {
                if (existingHashes.Contains(commitData.hash))
                    continue;

                _ = ResolveUser(commitData.commit.author?.Email, commitData.commit.author?.Name);
                _ = ResolveUser(commitData.commit.committer?.Email, commitData.commit.committer?.Name);

                var commit = new Commit
                {
                    Hash = commitData.hash,
                    Message = commitData.commit.message,
                    authorEmail = commitData.commit.author?.Email ?? "unknown@unknown.com",
                    authorName = commitData.commit.author?.Name ?? "Unknown",
                    CreatedAt = commitData.CreatedAt.UtcDateTime,
                    repoName = repoName,
                    committerEmail = commitData.commit.committer?.Email ?? "unknown@unknown.com",
                    committerName = commitData.commit.committer?.Name ?? "Unknown"
                };

                newCommits.Add(commit);
                newStats.Add(await FetchCommitStatsAsync(repoName, commit.Hash));
            }

            var newUsersList = newUsersDict.Values.ToList();

            if (newUsersList.Count > 0)
                await _dbContext.Users.AddRangeAsync(newUsersList);
            if (newStats.Count > 0)
                await _dbContext.CommitStats.AddRangeAsync(newStats);
            if (newCommits.Count > 0)
                await _dbContext.Commits.AddRangeAsync(newCommits);

            if (newUsersList.Count > 0 || newStats.Count > 0 || newCommits.Count > 0)
                await _dbContext.SaveChangesAsync();

            return ServiceResult<SyncResultDto>.Ok(new SyncResultDto
            {
                Message = $"Added {newUsersList.Count} users, {newCommits.Count} commits, {newStats.Count} stats",
                UsersAdded = newUsersList.Count,
                CommitsAdded = newCommits.Count,
                CommitStatsAdded = newStats.Count
            });

            User? ResolveUser(string? email, string? name)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                if (existingUsers.TryGetValue(email, out var user))
                    return user;

                if (newUsersDict.TryGetValue(email, out user))
                    return user;

                user = new User
                {
                    UserEmail = email,
                    UserName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name
                };
                newUsersDict[email] = user;
                return user;
            }
        }
        catch (Exception ex)
        {
            return ServiceResult<SyncResultDto>.Fail(ex.Message, Application.Common.StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<ServiceResult<int>> SyncBranchesAsync(string repoName, string branch)
    {
        var branches = await _giteaClient.GetBranchesAsync(repoName);
        if (branches is null)
            return ServiceResult<int>.Ok(0);

        var requestedBranches = branches.Where(b => b.Name == branch).ToArray();
        if (requestedBranches.Length == 0)
            return ServiceResult<int>.Fail($"Branch {branch} not found", Application.Common.StatusCodes.Status400BadRequest);

        var branchNames = requestedBranches.Select(b => b.Name).ToArray();
        var existingBranches = await _dbContext.Branches
            .Where(b => branchNames.Contains(b.BranchName))
            .Select(b => b.BranchName)
            .ToHashSetAsync();

        var newBranches = requestedBranches
            .Where(b => !existingBranches.Contains(b.Name))
            .Select(b => new Branch
            {
                BranchName = b.Name,
                repoName = repoName,
                lastCommitHash = b.LastCommit.id
            })
            .ToList();

        if (newBranches.Count > 0)
        {
            await _dbContext.Branches.AddRangeAsync(newBranches);
            await _dbContext.SaveChangesAsync();
        }

        return ServiceResult<int>.Ok(newBranches.Count);
    }

    private async Task<CommitStats> FetchCommitStatsAsync(string repoName, string hash)
    {
        var commit = await _giteaClient.GetCommitByHashAsync(repoName, hash);
        return new CommitStats
        {
            CommitHash = commit.hash,
            ChangedFiles = commit.files.Count,
            AddedLines = commit.stats.AddedLines,
            DeletedLines = commit.stats.RemovedLines,
            TotalChanges = commit.stats.TotalChanged
        };
    }

    private static (DateTime startUtc, DateTime endUtc) NormalizeRange(DateTimeOffset start, DateTimeOffset end)
    {
        var startUtc = DateTime.SpecifyKind(start.UtcDateTime, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.UtcDateTime, DateTimeKind.Utc);
        return (startUtc, endUtc);
    }

    private static double CalculatePercentile(List<int> data, double percentile)
    {
        if (data.Count == 0) return 0;

        data.Sort();
        var index = (percentile / 100.0) * (data.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return data[lower];

        return data[lower] + (data[upper] - data[lower]) * (index - lower);
    }
}
