using System.Text;
using CodeMetrics.Application.DTOs.Analytics;

namespace CodeMetrics.Infrastructure.Ollama;

/// <summary>
/// Сжимает DTO метрик до компактного промпта (~500–900 символов) для быстрого префилла на CPU.
/// </summary>
public static class PerformanceMetricsCompactor
{
    public static string BuildCompactPayload(
        string authorEmail,
        AuthorSummaryDto summary,
        AuthorPerformanceDto performance,
        ProjectMetricsDto project)
    {
        var sb = new StringBuilder(768);
        var email = authorEmail.Trim();

        sb.AppendLine($"[author] {email}");
        sb.AppendLine($"[period] {summary.Period.Start}..{summary.Period.End}");

        var weeks = summary.Weekly.Take(12).ToList();
        if (weeks.Count > 0)
        {
            var counts = string.Join(",", weeks.Select(w => w.CommitCount));
            sb.AppendLine($"[weekly_commits] {counts} (last_week={weeks[^1].CommitCount})");
        }

        var periodCommits = weeks.Sum(w => w.CommitCount);
        sb.AppendLine($"[period_commits] {periodCommits} [avg_change_lines] {summary.AverageChangeSize:F0}");

        var bestDay = summary.BestDays.OrderByDescending(d => d.Commits).FirstOrDefault();
        if (bestDay is not null)
            sb.AppendLine($"[best_day] {bestDay.Day} ({bestDay.Commits} commits)");

        var bestHour = summary.BestHours.OrderByDescending(h => h.Commits).FirstOrDefault();
        if (bestHour is not null)
            sb.AppendLine($"[best_hour] {bestHour.Hour}:00 ({bestHour.Commits} commits)");

        sb.AppendLine(
            $"[speed_30d] commits={performance.Speed.Commits} useful_lines={performance.Speed.UsefulLines}");

        if (performance.Stability.Weeks.Count > 0)
        {
            var list = performance.Stability.Weeks;
            sb.AppendLine(
                $"[stability_5w] {string.Join(",", list)} min={list.Min()} max={list.Max()} avg={list.Average():F1}");
        }

        var norm = performance.Normalization;
        sb.AppendLine(
            $"[team_norm] commits_p5-p95={norm.MinCommits}-{norm.MaxCommits} lines_p5-p95={norm.MinLines}-{norm.MaxLines}");

        sb.AppendLine(
            $"[project] repos={project.RepositoryCount} total_commits={project.TotalCommits} total_lines={project.TotalChangedLines}");

        var authorEntry = project.ActiveCommitters
            .FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));

        var authorCommits = authorEntry?.CommitCount ?? 0;
        var authorLines = authorEntry?.TotalChangedLines ?? 0;
        var commitCounts = project.ActiveCommitters.Select(a => a.CommitCount).Where(c => c > 0).ToList();
        var lineCounts = project.ActiveCommitters.Select(a => a.TotalChangedLines).Where(c => c > 0).ToList();
        var teamAvgCommits = commitCounts.Count > 0 ? commitCounts.Average() : 0;
        var commitRank = RankDesc(commitCounts, authorCommits);
        var lineRank = RankDesc(lineCounts, authorLines);

        sb.AppendLine(
            $"[author_in_team] commits={authorCommits} lines={authorLines} " +
            $"share_commits={(project.TotalCommits > 0 ? 100.0 * authorCommits / project.TotalCommits : 0):F1}% " +
            $"vs_avg_commits={(teamAvgCommits > 0 ? 100.0 * authorCommits / teamAvgCommits : 0):F1}% " +
            $"rank_commits={commitRank}/{commitCounts.Count} rank_lines={lineRank}/{lineCounts.Count}");

        return sb.ToString().TrimEnd();
    }

    private static string RankDesc(List<int> values, int value)
    {
        if (values.Count == 0) return "0";
        var sorted = values.OrderByDescending(v => v).ToList();
        var rank = sorted.FindIndex(v => v <= value);
        return rank < 0 ? $"{values.Count}" : $"{rank + 1}";
    }
}
