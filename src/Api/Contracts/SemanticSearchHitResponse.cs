namespace Api.Contracts;

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
