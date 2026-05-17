using CodeMetrics.Application.Common;
using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Ollama;
using CodeMetrics.Infrastructure.Ollama;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetrics.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class OlamaController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly ICodeMetricsService _metricsService;
    private readonly ISonarQubeService _sonarService;

    public OlamaController(
        IOllamaService ollamaService,
        ICodeMetricsService metricsService,
        ISonarQubeService sonarService)
    {
        _ollamaService = ollamaService;
        _metricsService = metricsService;
        _sonarService = sonarService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<OllamaTextResponseDto>> Ask(
        [FromBody] OllamaTextRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Вопрос не может быть пустым" });

        if (request.MetricsStartDate.HasValue && request.MetricsEndDate.HasValue
            && request.MetricsEndDate < request.MetricsStartDate)
            return BadRequest(new { error = "MetricsEndDate не может быть раньше MetricsStartDate" });

        try
        {
            var context = await BuildAskContextAsync(request, cancellationToken);
            var answer = await _ollamaService.AskAsync(request.Text, context, cancellationToken);
            return Ok(new OllamaTextResponseDto { Text = answer });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "Ollama недоступен", details = ex.Message });
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<OllamaTextResponseDto>> Chat(
        [FromBody] OllamaTextRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _ollamaService.ChatAsync(request.Text, cancellationToken);
            return Ok(new OllamaTextResponseDto { Text = reply });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "Ollama недоступен", details = ex.Message });
        }
    }

    [HttpGet("analyze-performance")]
    public async Task<ActionResult<PerformanceAnalysisResponseDto>> AnalyzePerformance(
        [FromQuery] string email,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        [FromQuery] string? context = null,
        [FromQuery] string? sonarProjectKey = null,
        [FromQuery] string? sonarBranch = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email автора обязателен" });

        if (endDate < startDate)
            return BadRequest(new { error = "endDate не может быть раньше startDate" });

        var summary = await _metricsService.GetAuthorSummaryAsync(email, startDate, endDate);
        var performance = await _metricsService.GetAuthorPerformanceAsync(email, startDate, endDate);
        var project = await _metricsService.GetProjectMetricsAsync(startDate, endDate);

        var sonarScan = string.IsNullOrWhiteSpace(sonarProjectKey)
            ? null
            : await _sonarService.GetScanSummaryAsync(sonarProjectKey.Trim(), sonarBranch);

        try
        {
            var comment = await _ollamaService.AnalyzeAuthorPerformanceAsync(
                email, summary, performance, project, sonarScan, context, cancellationToken);

            return Ok(new PerformanceAnalysisResponseDto
            {
                Author = email,
                Period = new PeriodDto
                {
                    Start = startDate.ToString("yyyy-MM-dd"),
                    End = endDate.ToString("yyyy-MM-dd")
                },
                Comment = comment,
                Metrics = new PerformanceAnalysisMetricsDto
                {
                    Summary = summary,
                    Performance = performance,
                    Project = project
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "Ollama недоступен", details = ex.Message });
        }
    }

    [HttpGet("fine-tuning-guide")]
    public ActionResult<FineTuningGuideDto> GetFineTuningGuide() =>
        Ok(_ollamaService.GetFineTuningGuide());

    private async Task<string?> BuildAskContextAsync(
        OllamaTextRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var blocks = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.SonarProjectKey))
        {
            var scan = await _sonarService.GetScanSummaryAsync(
                request.SonarProjectKey.Trim(),
                request.SonarBranch);
            blocks.Add(SonarQubeMetricsCompactor.BuildCompactPayload(scan));
        }

        if (!string.IsNullOrWhiteSpace(request.MetricsAuthorEmail)
            && request.MetricsStartDate.HasValue
            && request.MetricsEndDate.HasValue)
        {
            var email = request.MetricsAuthorEmail.Trim();
            var start = request.MetricsStartDate.Value;
            var end = request.MetricsEndDate.Value;

            var summary = await _metricsService.GetAuthorSummaryAsync(email, start, end);
            var performance = await _metricsService.GetAuthorPerformanceAsync(email, start, end);
            var project = await _metricsService.GetProjectMetricsAsync(start, end);

            blocks.Add(PerformanceMetricsCompactor.BuildCompactPayload(email, summary, performance, project));
        }

        return blocks.Count == 0 ? null : string.Join("\n\n", blocks);
    }
}
