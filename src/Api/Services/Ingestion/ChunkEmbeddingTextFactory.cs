using Embeddings.Contracts;

namespace Api.Services.Ingestion;

internal sealed class ChunkEmbeddingTextFactory : IChunkEmbeddingTextFactory
{
    public TextEmbeddingRequest CreateDocumentRequest(MarkdownChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var lines = new List<string>
        {
            $"Title: {chunk.Title}"
        };

        if (!string.IsNullOrWhiteSpace(chunk.Section))
        {
            lines.Add($"Section: {chunk.Section}");
        }

        lines.Add("Content:");
        lines.Add(chunk.Content);

        return new TextEmbeddingRequest
        {
            Text = string.Join('\n', lines),
            Kind = EmbeddingKind.Document
        };
    }
}
