namespace CodeMetrics.Infrastructure.Ollama;

public sealed class OllamaInferenceOptions
{
    public int NumPredict { get; init; }
    public double Temperature { get; init; }
    public double TopP { get; init; }
    public int NumCtx { get; init; }

    public static OllamaInferenceOptions FromProfile(OllamaProfileOptions profile) => new()
    {
        NumPredict = profile.NumPredict,
        Temperature = profile.Temperature,
        TopP = profile.TopP,
        NumCtx = profile.NumCtx
    };
}
