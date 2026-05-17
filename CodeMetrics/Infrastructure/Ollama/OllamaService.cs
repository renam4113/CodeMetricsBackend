using System.Text;
using System.Text.Json;
using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Analytics;
using CodeMetrics.Application.DTOs.Ollama;
using CodeMetrics.Application.DTOs.SonarQube;
using Microsoft.Extensions.Options;

namespace CodeMetrics.Infrastructure.Ollama;

public sealed class OllamaService : IOllamaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OllamaOptions _options;

    public OllamaService(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public Task<string> AskAsync(string question, string? context = null, CancellationToken cancellationToken = default)
    {
        var prompt = BuildAskPrompt(question, context);
        return GenerateAsync(prompt, OllamaInferenceOptions.FromProfile(_options.Quick), cancellationToken);
    }

    public Task<string> ChatAsync(string message, CancellationToken cancellationToken = default) =>
        AskAsync(message, cancellationToken: cancellationToken);

    public Task<string> AnalyzeAuthorPerformanceAsync(
        string authorEmail,
        AuthorSummaryDto summary,
        AuthorPerformanceDto performance,
        ProjectMetricsDto projectMetrics,
        SonarScanSummaryDto? sonarScan = null,
        string? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var compactMetrics = PerformanceMetricsCompactor.BuildCompactPayload(
            authorEmail, summary, performance, projectMetrics);

        var sonarBlock = sonarScan is null
            ? string.Empty
            : "\n" + SonarQubeMetricsCompactor.BuildCompactPayload(sonarScan);

        var prompt = BuildAnalysisPrompt(authorEmail, compactMetrics + sonarBlock, additionalContext);

        return GenerateAsync(
            prompt,
            OllamaInferenceOptions.FromProfile(_options.Analysis),
            cancellationToken);
    }

    public FineTuningGuideDto GetFineTuningGuide() => new()
    {
        CurrentModel = _options.Model,
        CanFineTuneLocally = true,
        Summary =
            "Модель в Ollama (phi3:mini и аналоги) — предобученная база; «дообучение» локально возможно, " +
            "но для продуктивного качества на 8 GiB CPU реалистичнее RAG, Modelfile и лёгкий LoRA, чем полный fine-tune.",
        Recommendations =
        [
            "Для ускорения инференса на CPU используйте q4-квантование и модели 1–3B (phi3:mini, gemma2:2b, tinyllama).",
            "Сжимайте промпт (как PerformanceMetricsCompactor) и ограничивайте num_predict/num_ctx в appsettings.",
            "Для «обучения» на ваших метриках предпочтительнее RAG: хранить эталонные отчёты и подмешивать их в prompt.",
            "Полный fine-tune 3B+ модели на CPU непрактичен; нужна GPU (16+ GiB VRAM) или облачный тренинг."
        ],
        Methods =
        [
            new FineTuningMethodDto
            {
                Name = "Modelfile (Ollama)",
                Description = "Создание производной модели с SYSTEM-промптом, temperature и template без изменения весов.",
                Difficulty = "Низкая"
            },
            new FineTuningMethodDto
            {
                Name = "LoRA / QLoRA",
                Description = "Дообучение адаптеров на парах prompt→ответ (ваши отчёты по метрикам). Экспорт в GGUF и импорт в Ollama.",
                Difficulty = "Средняя (нужна GPU)"
            },
            new FineTuningMethodDto
            {
                Name = "RAG",
                Description = "Не меняет веса: embeddings + поиск похожих кейсов успеваемости перед генерацией ответа.",
                Difficulty = "Низкая–средняя"
            },
            new FineTuningMethodDto
            {
                Name = "Полный fine-tune",
                Description = "Переобучение всех весов на доменном датасете. Для phi3:mini требует значительных ресурсов.",
                Difficulty = "Высокая"
            },
            new FineTuningMethodDto
            {
                Name = "Create model (ollama create)",
                Description = "Сборка кастомной модели из GGUF + параметры; удобно для закрепления стиля ответов наставника.",
                Difficulty = "Низкая–средняя"
            }
        ]
    };

    private async Task<string> GenerateAsync(
        string prompt,
        OllamaInferenceOptions inferenceOptions,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Ollama");

        var request = new
        {
            model = _options.Model,
            prompt,
            stream = false,
            options = new
            {
                num_predict = inferenceOptions.NumPredict,
                temperature = inferenceOptions.Temperature,
                top_p = inferenceOptions.TopP,
                num_ctx = inferenceOptions.NumCtx
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }

    private static string BuildAskPrompt(string question, string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return question.Trim();

        return $"""
            Ответьте на вопрос пользователя на русском языке, обращаясь на «Вы».
            Используйте приведённые данные как контекст, если они относятся к вопросу.

            Контекст:
            {context.Trim()}

            Вопрос:
            {question.Trim()}
            """;
    }

    private static string BuildAnalysisPrompt(
        string authorEmail,
        string compactMetrics,
        string? additionalContext)
    {
        var contextBlock = string.IsNullOrWhiteSpace(additionalContext)
            ? string.Empty
            : $"\nДополнительный контекст: {additionalContext.Trim()}";

        return $"""
            Вы — наставник разработчиков. Составьте отчёт на русском языке (2–4 абзаца, без markdown и списков).
            Обращайтесь к читателю на «Вы».

            Проанализируйте метрики Gitea и результаты сканирования SonarQube (если есть в данных).
            - При хороших показателях — отметьте сильные стороны.
            - При любых проблемах (даже если остальное хорошо) — обязательно дайте конкретную обратную связь: что исправить.
            - Укажите приоритетные действия по качеству кода, покрытию, багам и code smells.
            Опирайтесь только на факты из данных ниже.

            Автор: {authorEmail}
            {compactMetrics}{contextBlock}
            """;
    }
}
