using CodeMetrics.Application.Common;
using CodeMetrics.Application.DTOs.Analytics;

namespace CodeMetrics.Application.DTOs.Ollama;

public sealed class OllamaTextRequestDto
{
    public string Text { get; init; } = string.Empty;
}

public sealed class OllamaTextResponseDto
{
    public string Text { get; init; } = string.Empty;
}

public sealed class PerformanceAnalysisRequestDto
{
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string? Context { get; init; }
}

public sealed class PerformanceAnalysisMetricsDto
{
    public AuthorSummaryDto Summary { get; init; } = new();
    public AuthorPerformanceDto Performance { get; init; } = new();
    public ProjectMetricsDto Project { get; init; } = new();
}

public sealed class PerformanceAnalysisResponseDto
{
    public string Author { get; init; } = string.Empty;
    public PeriodDto Period { get; init; } = new();
    public string Comment { get; init; } = string.Empty;
    public PerformanceAnalysisMetricsDto Metrics { get; init; } = new();
}

public sealed class FineTuningMethodDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
}

public sealed class FineTuningGuideDto
{
    public string CurrentModel { get; init; } = string.Empty;
    public bool CanFineTuneLocally { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Recommendations { get; init; } = [];
    public IReadOnlyList<FineTuningMethodDto> Methods { get; init; } = [];
}
