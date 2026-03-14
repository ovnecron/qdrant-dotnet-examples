namespace Api.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public RagAnswerProvider AnswerProvider { get; set; } = RagAnswerProvider.Deterministic;

    public string? AnswerModel { get; set; }

    public string? BaseUrl { get; set; } = "http://localhost:11434/api";

    public string? ApiKey { get; set; }

    public float Temperature { get; set; } = 0.0f;

    public int MaxAnswerTokens { get; set; } = 256;

    public int RequestTimeoutSeconds { get; set; } = 30;
}
