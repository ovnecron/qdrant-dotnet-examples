namespace Embeddings.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; set; } = "Deterministic";

    public string Model { get; set; } = "hashing-text-v1";

    public int Dimension { get; set; } = 384;

    public int BatchSize { get; set; } = 16;

    public string SchemaVersion { get; set; } = "v1";

    public string? BaseUrl { get; set; } = "http://localhost:11434/api";

    public string? ApiKey { get; set; }

    public string? ApiVersion { get; set; }

    public string? DeploymentName { get; set; }
}
