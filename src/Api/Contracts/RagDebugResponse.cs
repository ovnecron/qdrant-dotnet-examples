namespace Api.Contracts;

public sealed record RagDebugResponse
{
    public required string Collection { get; init; }

    public required string EmbeddingProvider { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }

    public required string AnswerProvider { get; init; }

    public required string AnswerModel { get; init; }

    public required int RetrievedHitCount { get; init; }
}
