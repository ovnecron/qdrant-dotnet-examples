using Embeddings.Contracts;
using Embeddings.Interfaces;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;

namespace Api.Services;

internal sealed class TextRetrievalService : ITextRetrievalService
{
    private readonly ITextEmbeddingClient _embeddingClient;
    private readonly IVectorStoreClient _vectorStoreClient;

    public TextRetrievalService(
        ITextEmbeddingClient embeddingClient,
        IVectorStoreClient vectorStoreClient)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
    }

    public async Task<TextRetrievalResult> RetrieveAsync(
        TextRetrievalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var embedding = await _embeddingClient.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = request.QueryText,
                Kind = EmbeddingKind.Query
            },
            cancellationToken);

        var hits = await _vectorStoreClient.SearchAsync(
            new SearchRequest
            {
                CollectionName = request.CollectionName,
                QueryVector = embedding.Vector.ToArray(),
                TopK = request.TopK,
                MinScore = request.MinScore,
                Filter = request.Filter
            },
            cancellationToken);

        return new TextRetrievalResult(
            request.CollectionName,
            request.QueryText,
            embedding.Descriptor,
            hits);
    }
}
