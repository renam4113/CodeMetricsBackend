using CodeMetrics.Clients;
using CodeMetrics.Context;
using CodeMetrics.Entity;
using CodeMetrics.Models;
using CodeMetricsApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace CodeMetrics.Service
{
    public class CodeMetricsService : ICodeMetricsService
    {
        private readonly GiteaClient _client;
        private readonly CodeMetricsDbContext _dbContext;

        public CodeMetricsService(CodeMetricsDbContext dbContext, GiteaClient client)
        {
            _dbContext = dbContext;
            _client = client;
        }



        public async Task<CommitStats?> GetCommitStats(string reposKey, string hash)
        {
            var commit = await _client.GetCommitByHashAsync(reposKey, hash);
            return new CommitStats 
            { 
                CommitHash = commit.hash,
                ChangedFiles = commit.files.Count,
                AddedLines = commit.stats.AddedLines,
                DeletedLines = commit.stats.RemovedLines,
                TotalChanges = commit.stats.TotalChanged
            };
        }

        public async Task<List<string>> GetReposName()
        {
            var repos = await _client.GetReposAsync();

            if (repos is null)
            {
                return [];
            }

            return repos.Select(c => c.Name).ToList();



        }
        private async Task<ContentResult> UpdateBranches(string repoName, string? branch)
        {

                var branches = await _client.GetBranchesAsync(repoName);

                if (branches is null)
                {
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No branches to process",
                        ContentType = "application/json",
                    };
                }

                var requestedBranches = branches.Where(b => b.Name == branch).ToArray();

                if (requestedBranches.Length == 0)
                {
                    return new ContentResult
                    {
                        StatusCode = 400,
                        Content = $"Branch {branch} Not Found \n",
                        ContentType = "application/json",
                    };
                }

                var branchNames = requestedBranches.Select(c => c.Name).ToArray();

                var existingBranches = await _dbContext.Branches
                    .Where(c => branchNames.Contains(c.BranchName))
                    .Select(c => c.BranchName)
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

                if (newBranches.Count != 0)
                {
                    await _dbContext.Branches.AddRangeAsync(newBranches);
                    await _dbContext.SaveChangesAsync();
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {newBranches.Count} branches \n",
                    ContentType = "application/json",
                };
            
           
        }
        private async Task<ContentResult> UpdateRepos(string? branch, int limit)
        {
            try
            {
                var repos = await _client.GetReposAsync();

                if (repos is null)
                {
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No repos to process",
                        ContentType = "application/json",
                    };
                }

                var repoNames = repos.Select(c => c.Name).ToList();

                var existingRepos = await _dbContext.Repositories
                    .Where(r => repoNames.Contains(r.RepoName))
                    .Select(c => c.RepoName)
                    .ToHashSetAsync();

                var newRepos = new List<Repository>();

                foreach (var repo in repos)
                {
                    if (!existingRepos.Contains(repo.Name))
                    {
                        newRepos.Add(new Repository
                        {
                            RepoName = repo.Name,
                            OwnerName = "Test", // дописать
                            CreatedAt = repo.CreatedAt,
                            UpdatedAt = repo.UpdatedAt,
                            DefaultBranch = repo.DefaultBranch,
                            IsFork = true, // дописать
                        });

                        existingRepos.Add(repo.Name);
                    }
                }
                Console.Write("hello");
                if (newRepos.Count != 0)
                {
                    await _dbContext.Repositories.AddRangeAsync(newRepos);
                    await _dbContext.SaveChangesAsync();
                }

                foreach (var repo in repos)
                {
                    var result = await UpdateCommitsFromResponse(repo.Name);
                    if (result.StatusCode != 200)
                    {
                        return result;
                    }

                    var branchesResult = await UpdateBranches(repo.Name, branch);
                    if (branchesResult.StatusCode != 200)
                    {
                        return branchesResult;
                    }
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {newRepos.Count} repos \n",
                    ContentType = "application/json",
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating commits: {ex.Message}");

                return new ContentResult
                {
                    StatusCode = 500,
                    Content = $"Error: {ex.Message}",
                    ContentType = "application/json",
                };
            }
        }

        public async Task<object> GetAuthorPerformanceAsync(string authorName, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var now = DateTime.UtcNow;
            var mounthAgo = now.AddDays(-30);
            var fiveWeeksAgo = now.AddDays(-35);

            var lastWeekCommitsQuery = _dbContext.Commits
                .Where(c => c.authorEmail.ToLower() == authorName.ToLower()
                         && c.CreatedAt >= mounthAgo
                         && c.CreatedAt <= now);

            int commitsCountLastWeek = await lastWeekCommitsQuery.CountAsync();

            var lastWeekHashes = await lastWeekCommitsQuery
                .Select(c => c.Hash)
                .ToListAsync();

            int usefulLinesLastWeek = 0;
            if (lastWeekHashes.Any())
            {
                var stats = await _dbContext.CommitStats
                    .Where(s => lastWeekHashes.Contains(s.CommitHash))
                    .ToListAsync();

                usefulLinesLastWeek = stats.Sum(s => s.TotalChanges);
            }

            var fiveWeeksCommits = await _dbContext.Commits
                .Where(c => c.authorEmail.ToLower() == authorName.ToLower()
                         && c.CreatedAt >= fiveWeeksAgo
                         && c.CreatedAt <= now)
                .ToListAsync();

            var stability = Enumerable.Range(0, 5)
                .Select(i =>
                {
                    var startOfWeek = fiveWeeksAgo.AddDays(7 * i);
                    var endOfWeek = startOfWeek.AddDays(7);
                    return fiveWeeksCommits.Count(c => c.CreatedAt >= startOfWeek && c.CreatedAt < endOfWeek);
                })
                .ToList();

            IQueryable<Commit> companyCommitsQuery = _dbContext.Commits.AsQueryable();

            if (currentStartDate.HasValue)
            {
                companyCommitsQuery = companyCommitsQuery.Where(c => c.CreatedAt >= currentStartDate.Value.ToUniversalTime());
            }

            if (currentEndDate.HasValue)
            {
                companyCommitsQuery = companyCommitsQuery.Where(c => c.CreatedAt <= currentEndDate.Value.ToUniversalTime());
            }

            var commitsPerAuthor = await companyCommitsQuery
                .GroupBy(c => c.authorEmail)
                .Select(g => new { Author = g.Key, Count = g.Count() })
                .ToListAsync();

            var companyCommitHashes = await companyCommitsQuery
                .Select(c => new { c.authorEmail, c.Hash })
                .ToListAsync();

            var allStats = await _dbContext.CommitStats.ToListAsync();

            var linesByAuthor = companyCommitHashes
                .GroupJoin(
                    allStats,
                    c => c.Hash,
                    s => s.CommitHash,
                    (c, stats) => new
                    {
                        c.authorEmail,
                        TotalLines = stats.Sum(x => x.TotalChanges)
                    })
                .GroupBy(x => x.authorEmail)
                .Select(g => new
                {
                    Author = g.Key,
                    TotalLines = g.Sum(x => x.TotalLines)
                })
                .ToList();

            int maxCommits = (int)Math.Round(CalculatePercentile(commitsPerAuthor.Select(x => x.Count).ToList(), 95));
            int minCommits = (int)Math.Round(CalculatePercentile(commitsPerAuthor.Select(x => x.Count).ToList(), 5));

            int maxLines = (int)Math.Round(CalculatePercentile(linesByAuthor.Select(x => x.TotalLines).ToList(), 95));
            int minLines = (int)Math.Round(CalculatePercentile(linesByAuthor.Select(x => x.TotalLines).ToList(), 5));

            return new
            {
                Author = authorName,
                Speed = new
                {
                    Commits = commitsCountLastWeek,
                    Useful_Lines = usefulLinesLastWeek
                },
                Stability = new
                {
                    Weeks = stability
                },
                Normalization = new
                {
                    Max_Commits = maxCommits,
                    Min_Commits = minCommits,
                    Max_Lines = maxLines,
                    Min_Lines = minLines
                }
            };
        }

        private static double CalculatePercentile(List<int> data, double percentile)
        {
            if (!data.Any()) return 0;

            data.Sort();
            double index = (percentile / 100.0) * (data.Count - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);

            if (lower == upper)
                return data[lower];

            return data[lower] + (data[upper] - data[lower]) * (index - lower);
        }

        public async Task<ContentResult> UpdateDataBase(string? reposName, string? branch, int limit)
        {
            try
            {
                var result = await UpdateRepos(branch, limit);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating commits: {ex.Message}");

                return new ContentResult
                {
                    StatusCode = 500,
                    Content = $"Error: {ex.Message}",
                    ContentType = "application/json",
                };
            }
        }

        public async Task<ContentResult> GetCommitsByPeriod(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var repoNames = await _dbContext.Repositories
                .Select(r => r.RepoName)
                .ToListAsync();

            if (!repoNames.Any())
            {
                return new ContentResult
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(new List<object>()),
                    ContentType = "application/json"
                };
            }

            var commitsQuery = _dbContext.Commits
                .Where(c => repoNames.Contains(c.repoName));

            if (currentStartDate.HasValue)
            {
                commitsQuery = commitsQuery.Where(c => c.CreatedAt >= currentStartDate.Value);
            }

            if (currentEndDate.HasValue)
            {
                commitsQuery = commitsQuery.Where(c => c.CreatedAt <= currentEndDate.Value);
            }

            var resultList = await commitsQuery.ToListAsync();

            return new ContentResult
            {
                StatusCode = 200,
                Content = JsonConvert.SerializeObject(resultList, Formatting.Indented),
                ContentType = "application/json"
            };
        }

        public async Task<ContentResult> GetMetric(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var projectRepos = await _dbContext.Repositories
                .ToListAsync();

            var repoNames = projectRepos.Select(r => r.RepoName).ToList();

            var commitsQuery = _dbContext.Commits
                .Where(c => repoNames.Contains(c.repoName));

            if (currentStartDate.HasValue)
                commitsQuery = commitsQuery.Where(c => c.CreatedAt >= currentStartDate.Value);

            if (currentEndDate.HasValue)
                commitsQuery = commitsQuery.Where(c => c.CreatedAt <= currentEndDate.Value);

            var commits = await commitsQuery.ToListAsync();
            var commitHashes = commits.Select(c => c.Hash).ToList();

            var commitStats = await _dbContext.CommitStats
                .Where(s => commitHashes.Contains(s.CommitHash))
                .ToListAsync();

            var authors = commits
                .GroupBy(c => new { c.authorEmail, c.authorName })
                .Select(g => new
                {
                    name = g.Key.authorName,
                    email = g.Key.authorEmail,
                    commitCount = g.Count(),
                    totalChangedLines = g.Sum(commit =>
                    {
                        var stat = commitStats.FirstOrDefault(s => s.CommitHash == commit.Hash);
                        return stat?.AddedLines + stat?.DeletedLines ?? 0;
                    })
                })
                .OrderByDescending(a => a.commitCount)
                .ToList();

            var totalCommits = commits.Count;
            var totalChangedLines = commitStats.Sum(s => s.AddedLines + s.DeletedLines);

            var resultObject = new JObject();
            resultObject["Project"] = "Test";
            resultObject["Period"] = new JObject
            {
                ["Start"] = startDate.ToString("yyyy-MM-dd"),
                ["End"] = endDate.ToString("yyyy-MM-dd")
            };
            resultObject["ActiveCommitters"] = JArray.FromObject(authors);
            resultObject["TotalCommits"] = totalCommits;
            resultObject["TotalChangedLines"] = totalChangedLines;
            resultObject["RepositoryCount"] = projectRepos.Count;

            return new ContentResult
            {
                StatusCode = 200,
                Content = JsonConvert.SerializeObject(resultObject, Formatting.Indented),
                ContentType = "application/json"
            };
        }

        public async Task<ContentResult> GetRepoCommitsByPeriod(string name, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var repository = await _dbContext.Repositories
                .FirstOrDefaultAsync(r => r.RepoName == name);

            if (repository == null)
            {
                return new ContentResult
                {
                    StatusCode = 404,
                    Content = JsonConvert.SerializeObject(new { error = "Repository not found" }),
                    ContentType = "application/json"
                };
            }

            var commitsQuery = _dbContext.Commits
                .Where(c => c.repoName == repository.RepoName);

            if (currentStartDate.HasValue)
            {
                commitsQuery = commitsQuery.Where(c => c.CreatedAt >= currentStartDate.Value);
            }

            if (currentEndDate.HasValue)
            {
                commitsQuery = commitsQuery.Where(c => c.CreatedAt <= currentEndDate.Value);
            }

            var resultList = await commitsQuery.ToListAsync();

            return new ContentResult
            {
                StatusCode = 200,
                Content = JsonConvert.SerializeObject(resultList, Formatting.Indented),
                ContentType = "application/json"
            };
        }

        private async Task<ContentResult> UpdateCommitsFromResponse(string reposKey)
        {
            try
            {
                var commitsResponse = new List<CommitResponse>();

                var res = await _client.GetCommitsAsync(reposKey);
                if (res != null)
                {
                    commitsResponse.AddRange(res);
                }

                if (commitsResponse.Count == 0)
                {
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No commits to process",
                        ContentType = "application/json",
                    };
                }

                var commitHashes = commitsResponse.Select(c => c.hash).ToList();

                var existingHashes = await _dbContext.Commits
                    .Where(c => commitHashes.Contains(c.Hash))
                    .Select(c => c.Hash)
                    .ToHashSetAsync();

                var commitStatsHash = await _dbContext.CommitStats
                    .Select(p => p.CommitHash)
                    .ToHashSetAsync();

                var newCommits = new List<Commit>();
                var newStats = new List<CommitStats>();
                var newUsersDict = new Dictionary<string, Entity.User>(StringComparer.OrdinalIgnoreCase);
                var existingUsers = await _dbContext.Users
                    .ToDictionaryAsync(u => u.UserEmail, u => u);

                foreach (var commitData in commitsResponse)
                {
                    if (existingHashes.Contains(commitData.hash))
                        continue;

                    var committerUser = ResolveUser(commitData.commit.committer?.Email, commitData.commit.committer?.Name);
                    var authorUser = ResolveUser(commitData.commit.author?.Email, commitData.commit.author?.Name);

                    var commit = new Commit
                    {
                        Hash = commitData.hash,
                        Message = commitData.commit.message,
                        authorEmail = authorUser?.UserEmail ?? commitData.commit.author?.Email ?? "unknown@unknown.com",
                        authorName = authorUser?.UserName ?? commitData.commit.author?.Name ?? "Unknown",
                        CreatedAt = commitData.CreatedAt.UtcDateTime,
                        repoName = reposKey,
                        committerEmail = committerUser?.UserEmail ?? commitData.commit.committer?.Email ?? "unknown@unknown.com",
                        committerName = committerUser?.UserName ?? commitData.commit.committer?.Name ?? "Unknown"
                    };

                    newCommits.Add(commit);
                    newStats.Add(await GetCommitStats(reposKey, commit.Hash));
                }

                var newUsersList = newUsersDict.Values.ToList();

                if (newUsersList.Count != 0)
                {
                    await _dbContext.Users.AddRangeAsync(newUsersList);
                }

                if (newStats.Count != 0)
                {
                    await _dbContext.CommitStats.AddRangeAsync(newStats);
                }

                if (newCommits.Count != 0)
                {
                    await _dbContext.Commits.AddRangeAsync(newCommits);
                }

                if (newUsersList.Count != 0 || newStats.Count != 0 || newCommits.Count != 0)
                {
                    await _dbContext.SaveChangesAsync();
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {newUsersList.Count} users, {newCommits.Count} commits and {newStats.Count} commitStats",
                    ContentType = "plain/text",
                };

                Entity.User ResolveUser(string? email, string? name)
                {
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        return null;
                    }

                    if (existingUsers.TryGetValue(email, out var user))
                    {
                        return user;
                    }

                    if (newUsersDict.TryGetValue(email, out user))
                    {
                        return user;
                    }

                    user = new Entity.User
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
                Console.WriteLine($"Error updating commits: {ex.Message}");

                return new ContentResult
                {
                    StatusCode = 500,
                    Content = $"Error: {ex.Message}",
                    ContentType = "text/plain",
                };
            }
        }

        public async Task<ContentResult> GetCommits(string reposKey)
        {
            try
            {
                var resultList = new List<CommitWithStatsResponse>();

                var commits = await _dbContext.Commits
                    .Where(c => c.repoName == reposKey)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var commit in commits)
                {
                    var commitStats = await _dbContext.CommitStats
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cs => cs.CommitHash == commit.Hash);

                    var result = new CommitWithStatsResponse
                    {
                        Commit = new CommitDto
                        {
                            Hash = commit.Hash,
                            Message = commit.Message,
                            authorEmail = commit.authorEmail,
                            authorName = commit.authorName,
                            committerEmail = commit.committerEmail,
                            committerName = commit.committerName,
                            CreatedAt = commit.CreatedAt,
                            repoName = commit.repoName,
                            CommitStats = new CommitStatsDto
                            {
                                AddedLines = commitStats?.AddedLines ?? 0,
                                DeletedLines = commitStats?.DeletedLines ?? 0,
                                ChangedFiles = commitStats?.ChangedFiles ?? 0
                            }
                        }
                    };

                    resultList.Add(result);
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(resultList, Formatting.Indented),
                    ContentType = "application/json",
                };
            }
            catch (Exception ex)
            {

                return new ContentResult
                {
                    StatusCode = 500,
                    Content = $"Error: {ex.Message}",
                    ContentType = "application/json",
                };
            }
        }
        public async Task<object> GetAuthorSummaryAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;
            var commitsQuery = _dbContext.Commits.Where(c => c.authorEmail == authorEmail);

            if (currentStartDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(currentStartDate.Value, DateTimeKind.Utc);
                commitsQuery = commitsQuery.Where(c => c.CreatedAt >= startUtc);
            }

            if (currentEndDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(currentEndDate.Value, DateTimeKind.Utc);
                commitsQuery = commitsQuery.Where(c => c.CreatedAt <= endUtc);
            }

            var commits = await commitsQuery.ToListAsync();

            var commitHashes = commits.Select(c => c.Hash).ToList();
            var stats = await _dbContext.CommitStats
                .Where(s => commitHashes.Contains(s.CommitHash))
                .ToListAsync();

            var weekly = commits
                .GroupBy(c => new { Year = c.CreatedAt.Year, Week = ISOWeek.GetWeekOfYear(c.CreatedAt.LocalDateTime) })
                .Select(g =>
                {
                    int year = g.Key.Year;
                    int week = g.Key.Week;

                    DateTime startOfWeek = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                    DateTime endOfWeek = startOfWeek.AddDays(6);

                    return new
                    {
                        Year = year,
                        Week = week,
                        From = startOfWeek.ToString("yyyy-MM-dd"),
                        To = endOfWeek.ToString("yyyy-MM-dd"),
                        CommitCount = g.Count()
                    };
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Week)
                .ToList();

            double avgSize = stats.Count > 0 ? stats.Average(s => s.TotalChanges) : 0;

            var bestDays = commits
                .GroupBy(c => c.CreatedAt.DayOfWeek)
                .Select(g => new { Day = g.Key.ToString(), Commits = g.Count() })
                .OrderByDescending(g => g.Commits)
                .ToList();

            var bestHours = commits
                .GroupBy(c => c.CreatedAt.Hour)
                .Select(g => new { Hour = g.Key, Commits = g.Count() })
                .OrderByDescending(g => g.Commits)
                .ToList();

            return new
            {
                Author = authorEmail,
                Period = new
                {
                    Start = currentStartDate?.ToString("yyyy-MM-dd") ?? "Все время",
                    End = currentEndDate?.ToString("yyyy-MM-dd") ?? "Все время"
                },
                Weekly = weekly,
                AverageChangeSize = avgSize,
                BestDays = bestDays,
                BestHours = bestHours
            };
        }

        public async Task<ContentResult> GetCommit(string reposKey, string hash)
        {
            try
            {
                var commit = await _dbContext.Commits
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Hash == hash && c.repoName == reposKey);

                if (commit == null)
                {
                    return new ContentResult
                    {
                        StatusCode = 404,
                        Content = "Commit not found",
                        ContentType = "application/json",
                    };
                }

                var commitStats = await _dbContext.CommitStats
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cs => cs.CommitHash == hash);

                var result = new CommitWithStatsResponse
                {
                    Commit = new CommitDto
                    {
                        Hash = commit.Hash,
                        Message = commit.Message,
                        authorEmail = commit.authorEmail,
                        authorName = commit.authorName,
                        committerEmail = commit.committerEmail,
                        committerName = commit.committerName,
                        CreatedAt = commit.CreatedAt,
                        repoName = commit.repoName,
                        CommitStats = new CommitStatsDto
                        {
                            AddedLines = commitStats?.AddedLines ?? 0,
                            DeletedLines = commitStats?.DeletedLines ?? 0,
                            ChangedFiles = commitStats?.ChangedFiles ?? 0
                        }
                    }
                };

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(result, Formatting.Indented),
                    ContentType = "application/json",
                };
            }
            catch (Exception ex)
            {


                return new ContentResult
                {
                    StatusCode = 500,
                    Content = $"Error: {ex.Message}",
                    ContentType = "application/json",
                };
            }
        }

    }
}