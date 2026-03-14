using Api.Contracts;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal sealed class TextAnomalyResponseMapper : ITextAnomalyResponseMapper
{
    public TextAnomalyScoreResponse ToScoreResponse(
        string traceId,
        string collectionName,
        string text,
        bool includeNeighbors,
        bool includeDebug,
        EmbeddingDescriptor embeddingDescriptor,
        IReadOnlyList<SearchResult> neighbors,
        float threshold,
        AnomalyScoreComputation computation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(embeddingDescriptor);
        ArgumentNullException.ThrowIfNull(neighbors);
        ArgumentNullException.ThrowIfNull(computation);

        return new TextAnomalyScoreResponse
        {
            TraceId = traceId,
            Collection = collectionName,
            Text = text,
            AnomalyScore = computation.AnomalyScore,
            Threshold = threshold,
            IsAnomalous = computation.IsAnomalous,
            NeighborCount = neighbors.Count,
            MeanNeighborSimilarity = computation.MeanNeighborSimilarity,
            MaxNeighborSimilarity = computation.MaxNeighborSimilarity,
            Neighbors = includeNeighbors
                ? neighbors
                    .Select(
                        neighbor => new TextAnomalyNeighborResponse
                        {
                            Id = neighbor.ChunkId,
                            DocId = neighbor.DocId,
                            Source = neighbor.Source,
                            Title = neighbor.Title,
                            ContentPreview = CreateContentPreview(neighbor),
                            Score = neighbor.Score,
                            Tags = neighbor.Tags.ToArray()
                        })
                    .ToArray()
                : [],
            Debug = includeDebug
                ? new TextAnomalyDebugResponse
                {
                    EmbeddingProvider = embeddingDescriptor.Provider,
                    EmbeddingModel = embeddingDescriptor.Model,
                    EmbeddingSchemaVersion = embeddingDescriptor.SchemaVersion
                }
                : null
        };
    }

    private static string? CreateContentPreview(SearchResult neighbor)
    {
        if (!string.IsNullOrWhiteSpace(neighbor.ContentPreview))
        {
            return neighbor.ContentPreview;
        }

        if (string.IsNullOrWhiteSpace(neighbor.Content))
        {
            return null;
        }

        const int maxLength = 160;

        return neighbor.Content.Length <= maxLength
            ? neighbor.Content
            : $"{neighbor.Content[..maxLength].TrimEnd()}...";
    }
}
