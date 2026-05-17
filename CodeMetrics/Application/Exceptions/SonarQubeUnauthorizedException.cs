namespace CodeMetrics.Application.Exceptions;

public sealed class SonarQubeUnauthorizedException : Exception
{
    public SonarQubeUnauthorizedException(string message = "Токен SonarQube недействителен или отсутствует")
        : base(message)
    {
    }
}
