namespace Api.Services.Ingestion;

internal sealed record MarkdownIngestCommand(
    string CollectionName,
    string DocId,
    string SourceId,
    string? Title,
    string Markdown,
    IReadOnlyList<string> Tags,
    string? TenantId,
    int ChunkSize,
    int ChunkOverlap);
