namespace Api.Contracts;

public sealed record TextAnomalyDebugResponse
{
    public required string EmbeddingProvider { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }
}
