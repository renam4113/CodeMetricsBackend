using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Commits;
using CodeMetrics.Application.DTOs.Database;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Controllers;

[ApiController]
[Route("api/database")]
[EnableCors("AllowAll")]
public class CodeMetricsDatabaseController : ControllerBase
{
    private readonly ICodeMetricsDatabaseService _dbService;

    public CodeMetricsDatabaseController(ICodeMetricsDatabaseService databaseService) =>
        _dbService = databaseService;

    [HttpGet("repositories")]
    public async Task<ActionResult<IReadOnlyList<RepositoryDto>>> GetRepositories() =>
        Ok(await _dbService.GetRepositoriesAsync());

    [HttpGet("repositories/{repoName}")]
    public async Task<ActionResult<RepositoryDto>> GetRepository(string repoName)
    {
        var repository = await _dbService.GetRepositoryAsync(repoName);
        return repository is null ? NotFound(new { error = "Repository not found" }) : Ok(repository);
    }

    [HttpGet("repositories/{repoName}/branches")]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetBranches(string repoName) =>
        Ok(await _dbService.GetBranchesByRepoAsync(repoName));

    [HttpGet("repositories/{repoName}/commits")]
    public async Task<ActionResult<IReadOnlyList<CommitListItemDto>>> GetCommits(string repoName) =>
        Ok(await _dbService.GetCommitsByRepoAsync(repoName));

    [HttpGet("commits/{hash}")]
    public async Task<ActionResult<CommitListItemDto>> GetCommitByHash(string hash)
    {
        var commit = await _dbService.GetCommitByHashAsync(hash);
        return commit is null ? NotFound(new { error = "Commit not found" }) : Ok(commit);
    }

    [HttpGet("commits/{hash}/stats")]
    public async Task<ActionResult<CommitStatsRecordDto>> GetCommitStats(string hash)
    {
        var stats = await _dbService.GetCommitStatsByHashAsync(hash);
        return stats is null ? NotFound(new { error = "Commit stats not found" }) : Ok(stats);
    }

    [HttpGet("users/{email}")]
    public async Task<ActionResult<UserDto>> GetUser(string email)
    {
        var user = await _dbService.GetUserByEmailAsync(email);
        return user is null ? NotFound(new { error = "User not found" }) : Ok(user);
    }
}
