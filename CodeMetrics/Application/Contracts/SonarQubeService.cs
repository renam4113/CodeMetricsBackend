using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CodeMetrics.Application.DTOs.SonarQube;
using CodeMetrics.Application.Exceptions;
using CodeMetrics.Models;
using Microsoft.Extensions.Options;

namespace CodeMetrics.Application.Contracts;

public class SonarQubeService : ISonarQubeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly SonarQubeSettings _settings;
    private readonly ILogger<SonarQubeService> _logger;

    public SonarQubeService(
        IHttpClientFactory httpClientFactory,
        IOptions<SonarQubeSettings> settings,
        ILogger<SonarQubeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.Token);
    }

    public async Task<SonarQualityGateResult?> GetQualityGateAsync(string projectKey, string? branch = null)
    {
        var url = $"api/qualitygates/project_status?projectKey={Uri.EscapeDataString(projectKey)}";
        if (!string.IsNullOrEmpty(branch))
            url += $"&branch={Uri.EscapeDataString(branch)}";

        using var response = await SendAsync(() => _httpClient.GetAsync(url));
        var json = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(json).RootElement;

        return JsonSerializer.Deserialize<SonarQualityGateResult>(
            root.GetProperty("projectStatus").GetRawText(),
            JsonOptions);
    }

    public async Task<SonarMeasuresResult?> GetMeasuresAsync(
        string projectKey,
        string? branch = null,
        List<string>? metrics = null)
    {
        var requestedMetrics = NormalizeMetrics(metrics);
        var metricsStr = string.Join(",", requestedMetrics);

        var url = $"api/measures/component?component={Uri.EscapeDataString(projectKey)}&metricKeys={metricsStr}";
        if (!string.IsNullOrEmpty(branch))
            url += $"&branch={Uri.EscapeDataString(branch)}";

        using var response = await SendAsync(() => _httpClient.GetAsync(url));
        var json = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(json).RootElement;

        if (!root.TryGetProperty("component", out var componentElement))
            throw new SonarQubeNotFoundException($"Проект '{projectKey}' не найден в SonarQube");

        var result = JsonSerializer.Deserialize<SonarMeasuresResult>(
            componentElement.GetRawText(),
            JsonOptions);

        if (result is null)
            return null;

        ValidateRequestedMetrics(requestedMetrics, result.Measures);
        return result;
    }

    public async Task<List<SonarIssue>> GetIssuesAsync(
        string projectKey,
        string? branch = null,
        string? statuses = "OPEN,CONFIRMED")
    {
        var url = $"api/issues/search?componentKeys={Uri.EscapeDataString(projectKey)}&statuses={statuses}&ps=500";
        if (!string.IsNullOrEmpty(branch))
            url += $"&branch={Uri.EscapeDataString(branch)}";

        using var response = await SendAsync(() => _httpClient.GetAsync(url));
        var json = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(json).RootElement;

        return JsonSerializer.Deserialize<List<SonarIssue>>(
            root.GetProperty("issues").GetRawText(),
            JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<SonarMetricInfo>> GetAvailableMetricsAsync()
    {
        var metrics = new List<SonarMetricInfo>();
        var page = 1;
        const int pageSize = 500;

        while (true)
        {
            var url = $"api/metrics/search?ps={pageSize}&p={page}";
            using var response = await SendAsync(() => _httpClient.GetAsync(url));
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;
            var batch = JsonSerializer.Deserialize<List<SonarMetricInfo>>(
                root.GetProperty("metrics").GetRawText(),
                JsonOptions) ?? [];

            metrics.AddRange(batch);

            var total = root.TryGetProperty("paging", out var paging)
                && paging.TryGetProperty("total", out var totalElement)
                ? totalElement.GetInt32()
                : batch.Count;

            if (page * pageSize >= total || batch.Count == 0)
                break;

            page++;
        }

        return metrics.OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<SonarScanSummaryDto> GetScanSummaryAsync(string projectKey, string? branch = null)
    {
        var qualityGate = await GetQualityGateAsync(projectKey, branch);
        var measures = await GetMeasuresAsync(projectKey, branch);
        var issues = await GetIssuesAsync(projectKey, branch);

        return new SonarScanSummaryDto
        {
            ProjectKey = projectKey,
            Branch = branch,
            QualityGateStatus = qualityGate?.Status ?? "UNKNOWN",
            Measures = measures?.Measures ?? [],
            OpenIssuesCount = issues.Count,
            IssuesBySeverity = issues
                .GroupBy(i => i.Severity, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<bool> TriggerScanAsync(
        string projectKey,
        string projectPath,
        string? branch = null,
        Dictionary<string, string>? additionalParams = null)
    {
        try
        {
            var args = new List<string>
            {
                $"-Dsonar.projectKey={projectKey}",
                "-Dsonar.sources=.",
                $"-Dsonar.host.url={_settings.BaseUrl}",
                $"-Dsonar.token={_settings.Token}"
            };

            if (!string.IsNullOrEmpty(branch))
                args.Add($"-Dsonar.branch.name={branch}");

            if (additionalParams != null)
                foreach (var (key, value) in additionalParams)
                    args.Add($"-D{key}={value}");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _settings.ScannerPath,
                Arguments = string.Join(" ", args),
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process is null) return false;

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Sonar scan failed: {Error}", error);
                return false;
            }

            _logger.LogInformation("Sonar scan triggered for {ProjectKey}", projectKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger Sonar scan");
            return false;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> requestFactory)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token))
            throw new SonarQubeUnauthorizedException("Токен SonarQube отсутствует или не настроен");

        var response = await requestFactory();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            throw new SonarQubeUnauthorizedException();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            throw new SonarQubeNotFoundException("Ресурс не найден в SonarQube");
        }

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            var reason = response.ReasonPhrase;
            var body = await response.Content.ReadAsStringAsync();
            response.Dispose();
            throw new HttpRequestException(
                $"SonarQube API error: {(int)statusCode} {reason}. {body}",
                null,
                statusCode);
        }

        return response;
    }

    private static List<string> NormalizeMetrics(List<string>? metrics)
    {
        if (metrics is { Count: > 0 })
            return metrics.Select(m => m.Trim()).Where(m => !string.IsNullOrEmpty(m)).ToList();

        return ["coverage", "bugs", "vulnerabilities", "code_smells", "sqale_index", "ncloc"];
    }

    private static void ValidateRequestedMetrics(IReadOnlyList<string> requested, List<Measure> returned)
    {
        if (requested.Count == 0)
            return;

        var found = returned.Select(m => m.Metric).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requested.Where(m => !found.Contains(m)).ToList();
        if (missing.Count > 0)
            throw new SonarQubeNotFoundException($"Метрики не найдены: {string.Join(", ", missing)}");
    }
}
