using CodeMetrics.Entity;

namespace CodeMetrics.Service
{
    public interface ICodeMetricsDatabaseService
    {
        public Task<List<Project>> GetAllProjectsAsync();
        public Task<Project?> GetProjectByKeyAsync(string projectKey);
        public Task<List<Repository>> GetRepositoriesByProjectAsync(string projectKey);
        public Task<Repository?> GetRepositoryAsync(string repoName);
        public Task<List<Branch>> GetBranchesByRepoAsync(string repoName);
        public Task<List<Commit>> GetCommitsByRepoAsync(string repoName);
        public Task<Commit?> GetCommitByHashAsync(string hash);
        public Task<CommitStats?> GetCommitStatsByHashAsync(string commitHash);
        public Task<List<User>> GetAllUsersAsync();
        public Task<User?> GetUserByEmailAsync(string email);
    }
}
