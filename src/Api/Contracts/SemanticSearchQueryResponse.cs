namespace Api.Contracts;

public sealed record SemanticSearchQueryResponse
{
    public required string TraceId { get; init; }

    public required string Collection { get; init; }

    public required string QueryText { get; init; }

    public required string EmbeddingProvider { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }

    public required IReadOnlyList<SemanticSearchHitResponse> Hits { get; init; }
}

public sealed record SemanticSearchHitResponse
{
    public required string ChunkId { get; init; }

    public required string DocId { get; init; }

    public required float Score { get; init; }

    public required string Source { get; init; }

    public string? Title { get; init; }

    public string? Section { get; init; }

    public string? ContentPreview { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}
