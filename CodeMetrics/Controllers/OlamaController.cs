using CodeMetrics.Application.Common;
using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Ollama;
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

    public OlamaController(IOllamaService ollamaService, ICodeMetricsService metricsService)
    {
        _ollamaService = ollamaService;
        _metricsService = metricsService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<OllamaTextResponseDto>> Ask(
        [FromBody] OllamaTextRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new ErrorResponseDto("Вопрос не может быть пустым"));

        try
        {
            var answer = await _ollamaService.AskAsync(request.Text, cancellationToken);
            return Ok(new OllamaTextResponseDto { Text = answer });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new ErrorResponseDto("Ollama недоступен", ex.Message));
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
            return StatusCode(503, new ErrorResponseDto("Ollama недоступен", ex.Message));
        }
    }

    [HttpGet("analyze-performance")]
    public async Task<ActionResult<PerformanceAnalysisResponseDto>> AnalyzePerformance(
        [FromQuery] string email,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new ErrorResponseDto("Email автора обязателен"));

        if (endDate < startDate)
            return BadRequest(new ErrorResponseDto("endDate не может быть раньше startDate"));

        try
        {
            var summary = await _metricsService.GetAuthorSummaryAsync(email, startDate, endDate);
            var performance = await _metricsService.GetAuthorPerformanceAsync(email, startDate, endDate);
            var project = await _metricsService.GetProjectMetricsAsync(startDate, endDate);

            var comment = await _ollamaService.AnalyzeAuthorPerformanceAsync(
                email, summary, performance, project, context, cancellationToken);

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
            return StatusCode(503, new ErrorResponseDto("Ollama недоступен", ex.Message));
        }
    }

    /// <summary>
    /// Сводка о возможностях дообучения текущей модели Ollama.
    /// </summary>
    [HttpGet("fine-tuning-guide")]
    public ActionResult<FineTuningGuideDto> GetFineTuningGuide() =>
        Ok(_ollamaService.GetFineTuningGuide());
}
