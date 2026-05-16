namespace CodeMetrics.Application.Common;

public sealed class ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Details { get; init; }

    public ErrorResponseDto(string error, string? details = null)
    {
        Error = error;
        Details = details;
    }
}
