namespace CodeMetrics.Models;

public class SonarQubeSettings
{
    public required string BaseUrl { get; set; }
    public required string Token { get; set; }
    public string ProjectKey { get; set; } = "renamshina";
    public string ScannerPath { get; set; } = "sonar-scanner";
}
