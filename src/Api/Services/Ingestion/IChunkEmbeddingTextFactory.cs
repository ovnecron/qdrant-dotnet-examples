using Embeddings.Contracts;

namespace Api.Services.Ingestion;

internal interface IChunkEmbeddingTextFactory
{
    TextEmbeddingRequest CreateDocumentRequest(MarkdownChunk chunk);
}
