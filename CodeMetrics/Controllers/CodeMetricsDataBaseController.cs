using CodeMetrics.Service;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    [EnableCors("AllowAll")]
    public class CodeMetricsDatabaseController : ControllerBase
    {
        private readonly ICodeMetricsDatabaseService _dbService;
        public CodeMetricsDatabaseController(ICodeMetricsDatabaseService databaseService) => _dbService = databaseService;

        [HttpGet("/repository/project/{projectKey}")]
        public async Task<IActionResult> GetRepositoriesAsync()
        {
            var repositoriesByProject = await _dbService.GetRepositoriesAsync();
            return Ok(repositoriesByProject);
        }

        [HttpGet("/repository/name/{repoName}")]
        public async Task<IActionResult> GetRepository(string repoName)
        {
            var repository = await _dbService.GetRepositoryAsync(repoName);
            return Ok(repository);
        }

        [HttpGet("/branch/{repoName}")]
        public async Task<IActionResult> GetBranchesByRepo(string repoName)
        {
            var branchesByRepo = await _dbService.GetBranchesByRepoAsync(repoName);
            return Ok(branchesByRepo);
        }

        [HttpGet("/commit/name/{repoName}")]
        public async Task<IActionResult> GetCommitsByRepo(string repoName)
        {
            var commitsByRepo = await _dbService.GetCommitsByRepoAsync(repoName);
            return Ok(commitsByRepo);
        }

        [HttpGet("/commit/hash/{hash}")]
        public async Task<IActionResult> GetCommitByHash(string hash)
        {
            var commitByHash = await _dbService.GetCommitByHashAsync(hash);
            return Ok(commitByHash);
        }

        [HttpGet("/commitStats/{commitHash}")]
        public async Task<IActionResult> GetCommitStatsByHash(string commitHash)
        {
            var commitStatsByHash = await _dbService.GetCommitStatsByHashAsync(commitHash);
            return Ok(commitStatsByHash);
        }

        [HttpGet("/user/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var userByEmail = await _dbService.GetUserByEmailAsync(email);
            return Ok(userByEmail);
        }

    }
}
