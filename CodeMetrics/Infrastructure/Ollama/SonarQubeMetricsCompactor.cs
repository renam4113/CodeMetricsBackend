using System.Text;
using CodeMetrics.Application.DTOs.SonarQube;

namespace CodeMetrics.Infrastructure.Ollama;

public static class SonarQubeMetricsCompactor
{
    public static string BuildCompactPayload(SonarScanSummaryDto scan)
    {
        var sb = new StringBuilder(512);
        sb.AppendLine($"[sonar_project] {scan.ProjectKey}");
        if (!string.IsNullOrWhiteSpace(scan.Branch))
            sb.AppendLine($"[sonar_branch] {scan.Branch}");

        sb.AppendLine($"[quality_gate] {scan.QualityGateStatus}");

        foreach (var measure in scan.Measures)
            sb.AppendLine($"[metric] {measure.Metric}={measure.Value ?? "n/a"}");

        sb.AppendLine($"[open_issues] {scan.OpenIssuesCount}");
        if (scan.IssuesBySeverity.Count > 0)
        {
            var severities = string.Join(",",
                scan.IssuesBySeverity.Select(kv => $"{kv.Key}:{kv.Value}"));
            sb.AppendLine($"[issues_by_severity] {severities}");
        }

        return sb.ToString().TrimEnd();
    }
}
