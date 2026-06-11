using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.SonarQube;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class SonarQubeController : ControllerBase
{
    private readonly ISonarQubeService _sonarService;

    public SonarQubeController(ISonarQubeService sonarService) => _sonarService = sonarService;

    [HttpGet("quality-gate/{projectKey}")]
    public async Task<IActionResult> GetQualityGate(string projectKey, [FromQuery] string? branch)
    {
        var result = await _sonarService.GetQualityGateAsync(projectKey, branch);
        return result is null ? NotFound(new { error = $"Project '{projectKey}' not found in SonarQube" }) : Ok(result);
    }

    [HttpGet("measures/{projectKey}")]
    public async Task<IActionResult> GetMeasures(
        string projectKey,
        [FromQuery] string? branch,
        [FromQuery] List<string>? metrics)
    {
        var result = await _sonarService.GetMeasuresAsync(projectKey, branch, metrics);
        return result is null ? NotFound(new { error = $"Project '{projectKey}' not found in SonarQube" }) : Ok(result);
    }

    [HttpGet("issues/{projectKey}")]
    public async Task<IActionResult> GetIssues(
        string projectKey,
        [FromQuery] string? branch,
        [FromQuery] string statuses = "OPEN,CONFIRMED")
    {
        var issues = await _sonarService.GetIssuesAsync(projectKey, branch, statuses);
        return Ok(issues);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetAvailableMetrics()
    {
        var metrics = await _sonarService.GetAvailableMetricsAsync();
        return Ok(new
        {
            count = metrics.Count,
            commonExamples = SonarQubeMetricKeys.Common,
            metrics = metrics.Select(m => new { m.Key, m.Name, m.Type, m.Domain, m.Description })
        });
    }

    [HttpGet("scan-summary/{projectKey}")]
    public async Task<IActionResult> GetScanSummary(string projectKey, [FromQuery] string? branch)
    {
        var summary = await _sonarService.GetScanSummaryAsync(projectKey, branch);
        return Ok(summary);
    }

    [HttpPost("scan")]
    public async Task<IActionResult> TriggerScan([FromBody] ScanRequest request)
    {
        if (string.IsNullOrEmpty(request.ProjectKey) || string.IsNullOrEmpty(request.ProjectPath))
            return BadRequest(new { error = "ProjectKey and ProjectPath are required" });

        var success = await _sonarService.TriggerScanAsync(
            request.ProjectKey,
            request.ProjectPath,
            request.Branch,
            request.AdditionalParams);

        return success
            ? Ok(new ScanTriggerResult { Success = true, Message = "Scan triggered successfully" })
            : StatusCode(500, new ScanTriggerResult { Success = false, Message = "Failed to trigger scan" });
    }
}

public class ScanRequest
{
    public string ProjectKey { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public Dictionary<string, string>? AdditionalParams { get; set; }
}

public class ScanTriggerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
