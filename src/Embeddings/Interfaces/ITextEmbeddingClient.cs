using Embeddings.Contracts;

namespace Embeddings.Interfaces;

public interface ITextEmbeddingClient
{
    Task<TextEmbeddingResult> EmbedAsync(
        TextEmbeddingRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextEmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<TextEmbeddingRequest> requests,
        CancellationToken cancellationToken = default);
}
