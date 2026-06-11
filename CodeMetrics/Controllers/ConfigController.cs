using CodeMetrics.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeMetrics.Controllers;

[ApiController]
[Route("api/config")]
[EnableCors("AllowAll")]
public class ConfigController : ControllerBase
{
    private readonly SonarQubeSettings _sonarSettings;

    public ConfigController(IOptionsMonitor<SonarQubeSettings> sonarSettings) =>
        _sonarSettings = sonarSettings.CurrentValue;

    [HttpGet]
    public IActionResult GetConfig() =>
        Ok(new
        {
            sonarProjectKey = _sonarSettings.ProjectKey,
            sonarConfigured = !string.IsNullOrWhiteSpace(_sonarSettings.Token),
            sonarBaseUrl = _sonarSettings.BaseUrl
        });
}
