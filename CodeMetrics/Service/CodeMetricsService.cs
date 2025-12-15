using CodeMetrics.Entity;
using CodeMetrics.Clients;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CodeMetrics.Context;
using CodeMetricsApi.Models;
using Newtonsoft.Json;
using CodeMetrics.Models;
using System.Data;
using Newtonsoft.Json.Linq;

namespace CodeMetrics.Service
{
    public class CodeMetricsService : ICodeMetricsService
    {
        private SferaClient _client;
        private readonly CodeMetricsDbContext _dbContext;

        public CodeMetricsService(CodeMetricsDbContext dbContext)
        {
            _dbContext = dbContext;
            _client = new SferaClient();
        }



        private async Task<CommitStats> GetCommitStats(string projectKey, string reposKey, string hash)
        {
            try
            {
                var responseContent = await _client.GetCommitDiff(projectKey, reposKey, hash);

                var diffResponse = JsonConvert.DeserializeObject<CommitDiffApiResponse>(responseContent);

                if (diffResponse?.Data == null || string.IsNullOrEmpty(diffResponse.Data.Content))
                {

                    return null;
                }

                var base64Bytes = Convert.FromBase64String(diffResponse.Data.Content);
                var diffContent = Encoding.UTF8.GetString(base64Bytes);

                var stats = AnalyzeDiffContent(diffContent);

                return new CommitStats
                {
                    CommitHash = hash,
                    AddedLines = stats.addedLines,
                    DeletedLines = stats.deletedLines,
                    ChangedFiles = stats.changedFiles
                };
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public async Task<List<string>> GetReposNameByProject(string projectKey)
        {
            var reposResponse = await _client.GetRepositories(projectKey);
            var repos = JsonConvert.DeserializeObject<RepositoryApiResponse>(reposResponse);

            if (repos?.Data == null || !repos.Data.Any())
            {
                return [];
            }
            return repos.Data.Select(c => c.Name).ToList();



        }
        private async Task<ContentResult> UpdateBranches(string projectKey, string repoName, string? branch)
        {
            try
            {
                var branchesResponse = await _client.GetBranches(projectKey, repoName);
                var branches = JsonConvert.DeserializeObject<BranchApiResponse>(branchesResponse);


                if (branches?.Data == null || branches.Data.Length == 0)
                {
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No branches to process",
                        ContentType = "application/json",
                    };
                }


                var newBranches = new List<Branch>();

                List<string> branchesName = [];

                if (string.IsNullOrEmpty(branch))
                    branchesName = [.. branches.Data.Select(c => c.Name)];
                else
                    branchesName = [.. branches.Data.Where(b => b.Name == branch).Select(c => c.Name)];

                var existingBranches = await _dbContext.Branches
                    .Where(c => branchesName.Contains(c.BranchName))
                    .Select(c => c.BranchName)
                    .ToHashSetAsync();

                if (string.IsNullOrEmpty(branch))
                {
                    foreach (var branchData in branches.Data)
                    {
                        if (!existingBranches.Contains(branchData.Name))
                        {
                            var newbranch = new Branch()
                            {
                                BranchName = branchData.Name,
                                repoName = repoName,
                                lastCommitHash = branchData.LastCommit.hash
                            };
                            newBranches.Add(newbranch);
                        }
                    }
                }
                else
                {
                    var brachexist = await _dbContext.Branches.FirstOrDefaultAsync(p => p.BranchName == branch);
                    if (brachexist == null)
                    {
                        var branchfromresp = branches.Data.FirstOrDefault(p => p.Name == branch);
                        if (branchfromresp == null)
                        {
                            return new ContentResult
                            {
                                StatusCode = 400,
                                Content = $"Branch {branch} Not Found \n",
                                ContentType = "application/json",
                            };
                        }
                        var newbranch = new Branch()
                        {
                            BranchName = branchfromresp.Name,
                            repoName = repoName,
                            lastCommitHash = branchfromresp.LastCommit.hash
                        };
                        newBranches.Add(newbranch);
                    }
                }

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
        private async Task<ContentResult> UpdateRepos(string projectKey, string? branch, int limit)
        {
            try
            {
                int count = 0;
                var reposResponse = await _client.GetRepositories(projectKey);
                var repos = JsonConvert.DeserializeObject<RepositoryApiResponse>(reposResponse);


                if (repos?.Data == null || repos.Data.Length == 0)
                {
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No repos to process",
                        ContentType = "application/json",
                    };
                }


                var reposName = repos.Data.Select(c => c.Name).ToList();

                var existingRepos = await _dbContext.Repositories
                    .Where(r => reposName.Contains(r.RepoName))
                    .Select(c => c.RepoName)
                    .ToHashSetAsync();



                foreach (var repoData in repos.Data)
                {

                    if (!existingRepos.Contains(repoData.Name))
                    {
                        var repository = new Repository()
                        {
                            RepoName = repoData.Name,
                            OwnerName = repoData.OwnerName,
                            CreatedAt = repoData.CreatedAt,
                            UpdatedAt = repoData.UpdatedAt,
                            ProjectKey = projectKey,
                            DefaultBranch = repoData.DefaultBranch,
                            IsFork = repoData.IsFork
                        };

                        await _dbContext.Repositories.AddAsync(repository);
                        await _dbContext.SaveChangesAsync();
                        count++;
                    }

                    var result = await UpdateCommitsFromResponse(projectKey, repoData.Name, branch, limit);
                    if (result.StatusCode != 200)
                    {
                        return result;
                    }

                    var branchesResult = await UpdateBranches(projectKey, repoData.Name, branch);
                    if (branchesResult.StatusCode != 200)
                    {
                        return branchesResult;
                    }
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {count} repos \n",
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

        public async Task<object> GetAuthorPerformanceAsync(string authorEmail, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var now = DateTime.UtcNow;
            var mounthAgo = now.AddDays(-30);
            var fiveWeeksAgo = now.AddDays(-35);

            var lastWeekCommitsQuery = _dbContext.Commits
                .Where(c => c.authorEmail.ToLower() == authorEmail.ToLower()
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
                .Where(c => c.authorEmail.ToLower() == authorEmail.ToLower()
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
                Author = authorEmail,
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

        public async Task<ContentResult> UpdateDataBase(string? projectKey, string? reposName, string? branch, int limit)
        {
            try
            {
                var projectsResponse = await _client.GetProjects();
                var projects = JsonConvert.DeserializeObject<ProjectApiResponse>(projectsResponse)?.Data;

                if (projects == null || projects.Length == 0)
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = "No projects to process",
                        ContentType = "application/json",
                    };

                var newProjects = new List<Project>();
                var projectKeysInResp = projects?.Select(c => c.Name).ToList();

                var existingProjectsInDb = await _dbContext.Projects
                    .Where(c => projectKeysInResp.Contains(c.Name))
                    .Select(c => c.Name)
                    .ToHashSetAsync();
                if (string.IsNullOrEmpty(projectKey))
                {
                    foreach (var projectData in projects)
                    {
                        if (!existingProjectsInDb.Contains(projectData.Name))
                        {
                            var project = new Project()
                            {
                                ProjectKey = projectData.Name,
                                Name = projectData.Name,
                                Description = projectData.Description,
                                IsPublic = projectData.IsPublic,
                                CreatedAt = projectData.CreatedAt,
                                UpdatedAt = projectData.UpdatedAt
                            };

                            newProjects.Add(project);
                        }
                    }
                }
                else
                {
                    if (existingProjectsInDb.Contains(projectKey))
                    {
                        return new ContentResult
                        {
                            StatusCode = 200,
                            Content = "No projects to process",
                            ContentType = "application/json",
                        };
                    }
                    else
                    {
                        var pr = projects.FirstOrDefault(p => p.Name == projectKey);
                        var project = new Project()
                        {
                            ProjectKey = projectKey,
                            Name = projectKey,
                            Description = pr.Description,
                            IsPublic = pr.IsPublic,
                            CreatedAt = pr.CreatedAt,
                            UpdatedAt = pr.UpdatedAt
                        };

                        newProjects.Add(project);
                    }
                }


                if (newProjects.Count != 0)
                {
                    await _dbContext.Projects.AddRangeAsync(newProjects);
                    await _dbContext.SaveChangesAsync();
                    foreach (var pr in newProjects)
                    {
                        existingProjectsInDb.Add(pr.Name);
                    }
                }
                if (string.IsNullOrEmpty(projectKey))
                {
                    foreach (var project in existingProjectsInDb)
                    {
                        var result = await UpdateRepos(project, branch, limit);
                        if (result.StatusCode != 200)
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    var result = await UpdateRepos(projectKey, branch, limit);
                    if (result.StatusCode != 200)
                    {
                        return result;
                    }
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {newProjects.Count} projects \n",
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

        public async Task<ContentResult> GetProjectCommitsByPeriod(string name, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(r => r.Name == name);

            if (project == null)
            {
                return new ContentResult
                {
                    StatusCode = 404,
                    Content = JsonConvert.SerializeObject(new { error = "Project not found" }),
                    ContentType = "application/json"
                };
            }

            var repoNames = await _dbContext.Repositories
                .Where(r => r.ProjectKey == project.Name)
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
                commitsQuery = commitsQuery.Where(c => c.CreatedAt >= currentEndDate.Value);
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

        public async Task<ContentResult> GetMetricByProject(string name, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            DateTime? currentStartDate = startDate.UtcDateTime;
            DateTime? currentEndDate = endDate.UtcDateTime;

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(r => r.Name == name);

            if (project == null)
            {
                return new ContentResult
                {
                    StatusCode = 404,
                    Content = JsonConvert.SerializeObject(new { error = "Project not found" }),
                    ContentType = "application/json"
                };
            }

            var projectRepos = await _dbContext.Repositories
                .Where(r => r.ProjectKey == project.Name)
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
            resultObject["Project"] = name;
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

        private async Task<ContentResult> UpdateCommitsFromResponse(string projectKey, string reposKey, string? branch, int limit)
        {
            try
            {
                List<CommitData> commitsResponse = [];
                if (string.IsNullOrEmpty(branch))
                {
                    var branchesResp = JsonConvert.DeserializeObject<BranchApiResponse>(await _client.GetBranches(projectKey, reposKey));
                    foreach (var br in branchesResp.Data)
                    {
                        var commitsFromBranchResp = await _client.GetCommitsAsync(projectKey, reposKey, limit, br.Name);
                        commitsResponse.AddRange(commitsFromBranchResp.Data);
                    }
                }
                else
                {
                    var res = await _client.GetCommitsAsync(projectKey, reposKey, limit);
                    commitsResponse.AddRange(res.Data);
                }
                if (commitsResponse == null || commitsResponse.Count == 0)
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

                var newCommits = new List<Commit>();
                var newStats = new List<CommitStats>();
                var commitStatsHash = await _dbContext.CommitStats.Select(p => p.CommitHash).ToHashSetAsync();

                var newUsersDict = new Dictionary<string, Entity.User>();
                var existingUsersCache = new Dictionary<string, Entity.User>();
                var existingUsersInDb = await _dbContext.Users.ToListAsync();

                foreach (var commitData in commitsResponse)
                {
                    if (existingHashes.Contains(commitData.hash))
                        continue;

                    var committerEmail = commitData.committer?.Email ?? "";
                    Entity.User? committerUser = null;

                    if (!string.IsNullOrEmpty(committerEmail))
                    {

                        if (!existingUsersCache.TryGetValue(committerEmail, out committerUser))
                        {
                            committerUser = existingUsersInDb.FirstOrDefault(u => u.UserEmail == committerEmail);

                            if (committerUser != null)
                                existingUsersCache[committerEmail] = committerUser;
                        }

                        if (committerUser == null && !newUsersDict.ContainsKey(committerEmail))
                        {
                            committerUser = new Entity.User()
                            {
                                UserEmail = committerEmail,
                                UserName = commitData.committer?.Name ?? "Unknown"
                            };
                            newUsersDict[committerEmail] = committerUser;
                        }
                        else
                            committerUser ??= newUsersDict[committerEmail];
                    }

                    var authorEmail = commitData.author?.Email ?? "";
                    Entity.User? authorUser = null;

                    if (!string.IsNullOrEmpty(authorEmail))
                    {
                        if (!existingUsersCache.TryGetValue(authorEmail, out authorUser))
                        {
                            authorUser = existingUsersInDb.FirstOrDefault(u => u.UserEmail == authorEmail);

                            if (authorUser != null)
                                existingUsersCache[authorEmail] = authorUser;
                        }

                        if (authorUser == null && !newUsersDict.ContainsKey(authorEmail))
                        {
                            authorUser = new Entity.User()
                            {
                                UserEmail = authorEmail,
                                UserName = commitData.author?.Name ?? "Unknown"
                            };
                            newUsersDict[authorEmail] = authorUser;
                        }
                        else
                            authorUser ??= newUsersDict[authorEmail];
                    }

                    var commit = new Commit
                    {
                        Hash = commitData.hash,
                        Message = commitData.message,
                        authorEmail = commitData.author?.Email ?? "unknown@unknown.com",
                        authorName = commitData.author?.Name ?? "Unknown",
                        CreatedAt = commitData.created_at.LocalDateTime,
                        repoName = reposKey,
                        committerEmail = commitData.committer?.Email ?? "unknown@unknown.com",
                        committerName = commitData.committer?.Name ?? "Unknown"
                    };

                    newCommits.Add(commit);

                    var commitStats = await GetCommitStats(projectKey, reposKey, commit.Hash);

                    if (commitStats != null && !commitStatsHash.Contains(commitStats.CommitHash))
                    {
                        newStats.Add(commitStats);
                    }
                }

                var newUsersList = newUsersDict.Values.ToList();

                if (newUsersList.Count != 0)
                {
                    await _dbContext.Users.AddRangeAsync(newUsersList);
                    await _dbContext.SaveChangesAsync();
                }

                if (newStats.Count != 0)
                {
                    await _dbContext.CommitStats.AddRangeAsync(newStats);
                    await _dbContext.SaveChangesAsync();
                }

                if (newCommits.Count != 0)
                {
                    await _dbContext.Commits.AddRangeAsync(newCommits);
                    await _dbContext.SaveChangesAsync();
                }

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = $"Added {newUsersList.Count} users, {newCommits.Count} commits and {newStats.Count} commitStats",
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

        public async Task<ContentResult> GetCommits(string projectKey, string reposKey)
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
                .GroupBy(c => new { Year = c.CreatedAt.Year, Week = System.Globalization.ISOWeek.GetWeekOfYear(c.CreatedAt) })
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

        public async Task<ContentResult> GetCommit(string projectKey, string reposKey, string hash)
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

        private (int addedLines, int deletedLines, int changedFiles) AnalyzeDiffContent(string diffContent)
        {
            int addedLines = 0;
            int deletedLines = 0;
            int changedFiles = 0;

            var lines = diffContent.Split('\n');
            var processedFiles = new HashSet<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("diff --git"))
                {
                    var fileMatch = System.Text.RegularExpressions.Regex.Match(line, @"diff --git a/(.+) b/(.+)");
                    if (fileMatch.Success)
                    {
                        var currentFile = fileMatch.Groups[1].Value;
                        if (!processedFiles.Contains(currentFile))
                        {
                            processedFiles.Add(currentFile);
                            changedFiles++;
                        }
                    }
                }
                else if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    addedLines++;
                }
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                {
                    deletedLines++;
                }
            }

            return (addedLines, deletedLines, changedFiles);
        }
    }
}