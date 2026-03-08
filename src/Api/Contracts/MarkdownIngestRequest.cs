namespace Api.Contracts;

public sealed record MarkdownIngestRequest
{
    public string? Collection { get; init; }

    public string? DocId { get; init; }

    public string? SourceId { get; init; }

    public string? Title { get; init; }

    public string? Markdown { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? TenantId { get; init; }

    public int? ChunkSize { get; init; }

    public int? ChunkOverlap { get; init; }
}
