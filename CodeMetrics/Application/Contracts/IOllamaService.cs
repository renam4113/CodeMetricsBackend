using CodeMetrics.Application.DTOs.Analytics;
using CodeMetrics.Application.DTOs.Ollama;
using CodeMetrics.Application.DTOs.SonarQube;

namespace CodeMetrics.Application.Contracts;

public interface IOllamaService
{
    Task<string> AskAsync(string question, string? context = null, CancellationToken cancellationToken = default);
    Task<string> ChatAsync(string message, CancellationToken cancellationToken = default);
    Task<string> AnalyzeAuthorPerformanceAsync(
        string authorEmail,
        AuthorSummaryDto summary,
        AuthorPerformanceDto performance,
        ProjectMetricsDto projectMetrics,
        SonarScanSummaryDto? sonarScan = null,
        string? additionalContext = null,
        CancellationToken cancellationToken = default);
    FineTuningGuideDto GetFineTuningGuide();
}
