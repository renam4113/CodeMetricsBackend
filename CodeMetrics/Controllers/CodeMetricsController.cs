using CodeMetrics.Application.Common;
using CodeMetrics.Application.Contracts;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class CodeMetricsController : ControllerBase
{
    private readonly ICodeMetricsService _metricsService;

    public CodeMetricsController(ICodeMetricsService metricsService) => _metricsService = metricsService;

    [HttpGet("author/summary")]
    public async Task<ActionResult<Application.DTOs.Analytics.AuthorSummaryDto>> GetAuthorSummary(
        [FromQuery] string email,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate)
    {
        var result = await _metricsService.GetAuthorSummaryAsync(email, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("author/performance")]
    public async Task<ActionResult<Application.DTOs.Analytics.AuthorPerformanceDto>> GetAuthorPerformance(
        [FromQuery] string email,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate)
    {
        var result = await _metricsService.GetAuthorPerformanceAsync(email, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("ProjectMetric")]
    public async Task<ActionResult<Application.DTOs.Analytics.ProjectMetricsDto>> GetProjectMetric(
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate)
    {
        var result = await _metricsService.GetProjectMetricsAsync(startDate, endDate);
        return Ok(result);
    }

    [HttpGet("GetByPeriod")]
    public async Task<IActionResult> GetCommitsByPeriod(
        [FromQuery] string name,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate)
    {
        var result = await _metricsService.GetRepoCommitsByPeriodAsync(name, startDate, endDate);
        return FromServiceResult(result);
    }

    [HttpGet("Commits")]
    public async Task<IActionResult> GetCommits([FromQuery] string repoName)
    {
        var result = await _metricsService.GetCommitsAsync(repoName);
        return FromServiceResult(result);
    }

    [HttpGet("Commit")]
    public async Task<IActionResult> GetCommit(
        [FromQuery] string repoName,
        [FromQuery] string hash)
    {
        var result = await _metricsService.GetCommitAsync(repoName, hash);
        return FromServiceResult(result);
    }

    [HttpPost("UpdateDataBase")]
    public async Task<IActionResult> UpdateDataBase(
        [FromQuery] string? repoName,
        [FromQuery] string? branch,
        [FromQuery] int limit = 100)
    {
        var result = await _metricsService.SyncFromGiteaAsync(repoName, branch, limit);
        return FromServiceResult(result);
    }

    private IActionResult FromServiceResult<T>(ServiceResult<T> result)
    {
        if (result.Success)
            return Ok(result.Value);

        return StatusCode(result.StatusCode, new ErrorResponseDto(result.Error ?? "Unknown error"));
    }
}
