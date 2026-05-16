using CodeMetrics.Application.DTOs.Analytics;
using CodeMetrics.Application.DTOs.Ollama;

namespace CodeMetrics.Application.Contracts;

public interface IOllamaService
{
    Task<string> AskAsync(string question, CancellationToken cancellationToken = default);
    Task<string> ChatAsync(string message, CancellationToken cancellationToken = default);
    Task<string> AnalyzeAuthorPerformanceAsync(
        string authorEmail,
        AuthorSummaryDto summary,
        AuthorPerformanceDto performance,
        ProjectMetricsDto projectMetrics,
        string? additionalContext = null,
        CancellationToken cancellationToken = default);
    FineTuningGuideDto GetFineTuningGuide();
}
