using CodeMetrics.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeMetrics.Infrastructure.Filters;

public sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            SonarQubeUnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Message),
            SonarQubeNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(context.Exception, "Unhandled exception");

        var payload = new Dictionary<string, string?> { ["error"] = message };

        if (statusCode == StatusCodes.Status500InternalServerError && _environment.IsDevelopment())
            payload["details"] = context.Exception.Message;

        context.Result = new ObjectResult(payload) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
