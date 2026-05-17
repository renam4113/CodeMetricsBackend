namespace CodeMetrics.Application.DTOs.SonarQube;

/// <summary>
/// Часто используемые ключи метрик SonarQube для параметра measures.
/// Полный список — GET /api/sonarqube/metrics (загружается из SonarQube API).
/// </summary>
public static class SonarQubeMetricKeys
{
    public static readonly IReadOnlyList<string> Common =
    [
        "ncloc",
        "lines",
        "files",
        "directories",
        "projects",
        "coverage",
        "line_coverage",
        "branch_coverage",
        "bugs",
        "vulnerabilities",
        "security_hotspots",
        "code_smells",
        "sqale_index",
        "sqale_rating",
        "reliability_rating",
        "security_rating",
        "duplicated_lines_density",
        "duplicated_blocks",
        "complexity",
        "cognitive_complexity",
        "comment_lines_density",
        "alert_status",
        "quality_gate_details",
        "new_coverage",
        "new_bugs",
        "new_vulnerabilities",
        "new_code_smells",
        "new_duplicated_lines_density",
        "new_lines",
        "new_security_hotspots"
    ];
}
