namespace Api.Services.Ingestion;

internal interface IMarkdownChunker
{
    IReadOnlyList<MarkdownChunk> Chunk(
        MarkdownDocument document,
        MarkdownChunkingOptions? options = null);
}
