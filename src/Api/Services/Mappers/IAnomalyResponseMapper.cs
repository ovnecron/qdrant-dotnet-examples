using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface IAnomalyResponseMapper
{
    AnomalyScoreResponse ToScoreResponse(
        string traceId,
        string collectionName,
        bool includeNeighbors,
        IReadOnlyList<SearchResult> neighbors,
        float threshold,
        AnomalyScoreComputation computation);
}
