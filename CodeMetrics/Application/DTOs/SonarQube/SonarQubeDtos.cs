namespace CodeMetrics.Application.DTOs.SonarQube;

public class SonarQualityGateResult
{
    public string Status { get; set; } = string.Empty;
    public List<Condition> Conditions { get; set; } = new();
}

public class Condition
{
    public string MetricKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public string? ErrorThreshold { get; set; }
}

public class SonarMeasuresResult
{
    public string Name { get; set; } = string.Empty;
    public List<Measure> Measures { get; set; } = new();
}

public class Measure
{
    public string Metric { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? PeriodValue { get; set; }
}

public class SonarIssue
{
    public string Key { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string Component { get; set; } = string.Empty;
    public IssueTextRange? TextRange { get; set; }
}

public class IssueTextRange
{
    public int? StartLine { get; set; }
    public int? EndLine { get; set; }
    public int? StartOffset { get; set; }
    public int? EndOffset { get; set; }
}

public class SonarMetricInfo
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Domain { get; set; }
}

public class SonarScanSummaryDto
{
    public string ProjectKey { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string QualityGateStatus { get; set; } = string.Empty;
    public List<Measure> Measures { get; set; } = new();
    public int OpenIssuesCount { get; set; }
    public Dictionary<string, int> IssuesBySeverity { get; set; } = new();
}
