using CodeMetrics.Models;
using Microsoft.Extensions.Options;

namespace CodeMetrics.Infrastructure.SonarQube;

/// <summary>
/// Ожидает появления токена SonarQube в конфигурации (например, после init-services.sh).
/// </summary>
public sealed class SonarQubeTokenReadinessHostedService : BackgroundService
{
    private readonly IOptionsMonitor<SonarQubeSettings> _settings;
    private readonly ILogger<SonarQubeTokenReadinessHostedService> _logger;

    public SonarQubeTokenReadinessHostedService(
        IOptionsMonitor<SonarQubeSettings> settings,
        ILogger<SonarQubeTokenReadinessHostedService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.Token))
        {
            _logger.LogInformation("SonarQube token уже настроен");
            return;
        }

        _logger.LogWarning(
            "SonarQube token не задан. Эндпоинты /api/SonarQube/* будут возвращать 401 до установки SonarQube__Token");

        var deadline = DateTime.UtcNow.AddMinutes(10);

        while (!stoppingToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.Token))
            {
                _logger.LogInformation("SonarQube token обнаружен в конфигурации");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.CurrentValue.Token))
        {
            _logger.LogWarning(
                "SonarQube token так и не появился за 10 минут. Запустите scripts/init-services.sh и перезапустите backend");
        }
    }
}
