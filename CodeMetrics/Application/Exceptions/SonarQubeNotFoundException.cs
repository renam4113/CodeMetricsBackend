namespace CodeMetrics.Application.Exceptions;

public sealed class SonarQubeNotFoundException : Exception
{
    public SonarQubeNotFoundException(string message)
        : base(message)
    {
    }
}
