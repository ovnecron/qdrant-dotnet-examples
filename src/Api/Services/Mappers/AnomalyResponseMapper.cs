using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal sealed class AnomalyResponseMapper : IAnomalyResponseMapper
{
    public AnomalyScoreResponse ToScoreResponse(
        string traceId,
        string collectionName,
        bool includeNeighbors,
        IReadOnlyList<SearchResult> neighbors,
        float threshold,
        AnomalyScoreComputation computation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(neighbors);
        ArgumentNullException.ThrowIfNull(computation);

        return new AnomalyScoreResponse
        {
            TraceId = traceId,
            Collection = collectionName,
            AnomalyScore = computation.AnomalyScore,
            Threshold = threshold,
            IsAnomalous = computation.IsAnomalous,
            NeighborCount = neighbors.Count,
            MeanNeighborSimilarity = computation.MeanNeighborSimilarity,
            MaxNeighborSimilarity = computation.MaxNeighborSimilarity,
            Neighbors = includeNeighbors
                ? neighbors
                    .Select(
                        neighbor => new AnomalyNeighborResponse
                        {
                            Id = neighbor.ChunkId,
                            DocId = neighbor.DocId,
                            Source = neighbor.Source,
                            Score = neighbor.Score,
                            Tags = neighbor.Tags.ToArray()
                        })
                    .ToArray()
                : []
        };
    }
}
