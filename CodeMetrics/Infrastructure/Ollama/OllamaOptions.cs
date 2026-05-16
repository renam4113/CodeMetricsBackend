namespace CodeMetrics.Infrastructure.Ollama;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "phi3:mini";
    public OllamaProfileOptions Analysis { get; set; } = new();
    public OllamaProfileOptions Quick { get; set; } = new() { NumPredict = 180, NumCtx = 1536 };
}

public sealed class OllamaProfileOptions
{
    public int NumPredict { get; set; } = 280;
    public double Temperature { get; set; } = 0.5;
    public double TopP { get; set; } = 0.9;
    public int NumCtx { get; set; } = 2048;
}
