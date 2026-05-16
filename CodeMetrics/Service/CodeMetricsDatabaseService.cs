using CodeMetrics.Context;
using CodeMetrics.Entity;
using Microsoft.EntityFrameworkCore;

namespace CodeMetrics.Service
{
    public class CodeMetricsDatabaseService : ICodeMetricsDatabaseService
    {
        private readonly CodeMetricsDbContext _context;

        public CodeMetricsDatabaseService(CodeMetricsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Repository>> GetRepositoriesAsync()
        {
            return await _context.Repositories
                .OrderBy(r => r.RepoName)
                .ToListAsync();
        }

        public async Task<Repository?> GetRepositoryAsync(string repoName)
        {
            return await _context.Repositories
                .FirstOrDefaultAsync(r => r.RepoName == repoName);
        }

        public async Task<List<Branch>> GetBranchesByRepoAsync(string repoName)
        {
            return await _context.Branches
                .Where(b => b.repoName == repoName)
                .OrderBy(b => b.BranchName)
                .ToListAsync();
        }

        public async Task<List<Commit>> GetCommitsByRepoAsync(string repoName)
        {
            return await _context.Commits
                .Where(c => c.repoName == repoName)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }


        public async Task<Commit?> GetCommitByHashAsync(string hash)
        {
            return await _context.Commits
                .FirstOrDefaultAsync(c => c.Hash == hash);
        }

        public async Task<CommitStats?> GetCommitStatsByHashAsync(string commitHash)
        {
            return await _context.CommitStats
                .FirstOrDefaultAsync(cs => cs.CommitHash == commitHash);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == email);
        }

        

    }
}
