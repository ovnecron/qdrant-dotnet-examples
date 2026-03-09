using Api.Contracts;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface ISemanticSearchResponseMapper
{
    SemanticSearchQueryResponse ToQueryResponse(
        string traceId,
        string collectionName,
        string queryText,
        EmbeddingDescriptor embeddingDescriptor,
        IReadOnlyList<SearchResult> hits);
}

internal sealed class SemanticSearchResponseMapper : ISemanticSearchResponseMapper
{
    public SemanticSearchQueryResponse ToQueryResponse(
        string traceId,
        string collectionName,
        string queryText,
        EmbeddingDescriptor embeddingDescriptor,
        IReadOnlyList<SearchResult> hits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        ArgumentNullException.ThrowIfNull(embeddingDescriptor);
        ArgumentNullException.ThrowIfNull(hits);

        return new SemanticSearchQueryResponse
        {
            TraceId = traceId,
            Collection = collectionName,
            QueryText = queryText,
            EmbeddingProvider = embeddingDescriptor.Provider,
            EmbeddingModel = embeddingDescriptor.Model,
            EmbeddingSchemaVersion = embeddingDescriptor.SchemaVersion,
            Hits = hits
                .Select(
                    hit => new SemanticSearchHitResponse
                    {
                        ChunkId = hit.ChunkId,
                        DocId = hit.DocId,
                        Score = hit.Score,
                        Source = hit.Source,
                        Title = hit.Title,
                        Section = hit.Section,
                        ContentPreview = hit.ContentPreview,
                        Tags = hit.Tags
                    })
                .ToArray()
        };
    }
}
