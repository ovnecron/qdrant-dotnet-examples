namespace Api.Services.Ingestion;

internal sealed record MarkdownChunkingOptions
{
    public int ChunkSize { get; init; } = 800;

    public int ChunkOverlap { get; init; } = 120;
}
