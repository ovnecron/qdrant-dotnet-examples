using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface IVectorResponseMapper
{
    VectorUpsertResponse ToUpsertResponse(string collectionName, int upsertedCount);

    VectorSearchResponse ToSearchResponse(string traceId, IReadOnlyList<SearchResult> hits);
}

internal sealed class VectorResponseMapper : IVectorResponseMapper
{
    public VectorUpsertResponse ToUpsertResponse(string collectionName, int upsertedCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        return new VectorUpsertResponse
        {
            Collection = collectionName,
            UpsertedCount = upsertedCount
        };
    }

    public VectorSearchResponse ToSearchResponse(string traceId, IReadOnlyList<SearchResult> hits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentNullException.ThrowIfNull(hits);

        return new VectorSearchResponse
        {
            TraceId = traceId,
            Hits = hits
                .Select(
                    hit => new VectorSearchHitResponse
                    {
                        ChunkId = hit.ChunkId,
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
