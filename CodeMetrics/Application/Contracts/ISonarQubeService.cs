using CodeMetrics.Application.DTOs.SonarQube;

namespace CodeMetrics.Application.Contracts;

public interface ISonarQubeService
{
    Task<SonarQualityGateResult?> GetQualityGateAsync(string projectKey, string? branch = null);
    Task<SonarMeasuresResult?> GetMeasuresAsync(string projectKey, string? branch = null, List<string>? metrics = null);
    Task<List<SonarIssue>> GetIssuesAsync(string projectKey, string? branch = null, string? statuses = "OPEN,CONFIRMED");
    Task<bool> TriggerScanAsync(string projectKey, string projectPath, string? branch = null, Dictionary<string, string>? additionalParams = null);
    Task<IReadOnlyList<SonarMetricInfo>> GetAvailableMetricsAsync();
    Task<SonarScanSummaryDto> GetScanSummaryAsync(string projectKey, string? branch = null);
}
