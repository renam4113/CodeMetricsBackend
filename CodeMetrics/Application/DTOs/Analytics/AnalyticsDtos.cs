using System.Text.Json.Serialization;
using CodeMetrics.Application.Common;

namespace CodeMetrics.Application.DTOs.Analytics;

public sealed class WeeklyActivityDto
{
    public int Year { get; init; }
    public int Week { get; init; }
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public int CommitCount { get; init; }
}

public sealed class DayActivityDto
{
    public string Day { get; init; } = string.Empty;
    public int Commits { get; init; }
}

public sealed class HourActivityDto
{
    public int Hour { get; init; }
    public int Commits { get; init; }
}

public sealed class AuthorSummaryDto
{
    public string Author { get; init; } = string.Empty;
    public PeriodDto Period { get; init; } = new();
    public IReadOnlyList<WeeklyActivityDto> Weekly { get; init; } = [];
    public double AverageChangeSize { get; init; }
    public IReadOnlyList<DayActivityDto> BestDays { get; init; } = [];
    public IReadOnlyList<HourActivityDto> BestHours { get; init; } = [];
}

public sealed class SpeedMetricsDto
{
    public int Commits { get; init; }
    [JsonPropertyName("Useful_Lines")]
    public int UsefulLines { get; init; }
}

public sealed class StabilityMetricsDto
{
    public IReadOnlyList<int> Weeks { get; init; } = [];
}

public sealed class NormalizationMetricsDto
{
    [JsonPropertyName("Max_Commits")]
    public int MaxCommits { get; init; }
    [JsonPropertyName("Min_Commits")]
    public int MinCommits { get; init; }
    [JsonPropertyName("Max_Lines")]
    public int MaxLines { get; init; }
    [JsonPropertyName("Min_Lines")]
    public int MinLines { get; init; }
}

public sealed class AuthorPerformanceDto
{
    public string Author { get; init; } = string.Empty;
    public SpeedMetricsDto Speed { get; init; } = new();
    public StabilityMetricsDto Stability { get; init; } = new();
    public NormalizationMetricsDto Normalization { get; init; } = new();
}

public sealed class ActiveCommitterDto
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int CommitCount { get; init; }
    public int TotalChangedLines { get; init; }
}

public sealed class ProjectMetricsDto
{
    public string Project { get; init; } = "CodeMetrics";
    public PeriodDto Period { get; init; } = new();
    public IReadOnlyList<ActiveCommitterDto> ActiveCommitters { get; init; } = [];
    public int TotalCommits { get; init; }
    public int TotalChangedLines { get; init; }
    public int RepositoryCount { get; init; }
}

public sealed class SyncResultDto
{
    public string Message { get; init; } = string.Empty;
    public int RepositoriesAdded { get; init; }
    public int UsersAdded { get; init; }
    public int CommitsAdded { get; init; }
    public int CommitStatsAdded { get; init; }
    public int BranchesAdded { get; init; }
}
