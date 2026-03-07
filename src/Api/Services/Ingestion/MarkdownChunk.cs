namespace Api.Services.Ingestion;

internal sealed record MarkdownChunk
{
    public required string ChunkId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string DocId { get; init; }

    public required string Source { get; init; }

    public required string Title { get; init; }

    public string? Section { get; init; }

    public required string Content { get; init; }

    public required string Checksum { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}
