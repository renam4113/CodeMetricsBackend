namespace CodeMetrics.Application.Common;

public sealed class ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; } = StatusCodes.Status200OK;

    public static ServiceResult<T> Ok(T value) => new() { Success = true, Value = value };

    public static ServiceResult<T> Fail(string error, int statusCode) =>
        new() { Success = false, Error = error, StatusCode = statusCode };
}

public static class StatusCodes
{
    public const int Status200OK = 200;
    public const int Status400BadRequest = 400;
    public const int Status404NotFound = 404;
    public const int Status500InternalServerError = 500;
}
