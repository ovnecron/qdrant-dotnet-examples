using Api.Contracts;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface ITextAnomalyResponseMapper
{
    TextAnomalyScoreResponse ToScoreResponse(
        string traceId,
        string collectionName,
        string text,
        bool includeNeighbors,
        bool includeDebug,
        EmbeddingDescriptor embeddingDescriptor,
        IReadOnlyList<SearchResult> neighbors,
        float threshold,
        AnomalyScoreComputation computation);
}
