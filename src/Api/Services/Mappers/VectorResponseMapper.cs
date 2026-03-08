using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface IVectorResponseMapper
{
    VectorDeleteResponse ToDeleteResponse(string collectionName, int deletedCount);

    VectorRecordResponse ToRecordResponse(string collectionName, VectorRecord record);

    VectorUpsertResponse ToUpsertResponse(string collectionName, int upsertedCount);

    VectorSearchResponse ToSearchResponse(string traceId, IReadOnlyList<SearchResult> hits);
}

internal sealed class VectorResponseMapper : IVectorResponseMapper
{
    public VectorDeleteResponse ToDeleteResponse(string collectionName, int deletedCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        if (deletedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deletedCount));
        }

        return new VectorDeleteResponse
        {
            Collection = collectionName,
            DeletedCount = deletedCount
        };
    }

    public VectorRecordResponse ToRecordResponse(string collectionName, VectorRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(record);

        return new VectorRecordResponse
        {
            Collection = collectionName,
            ChunkId = record.ChunkId,
            Vector = record.Vector.ToArray(),
            DocId = record.DocId,
            Source = record.Source,
            Title = record.Title,
            Section = record.Section,
            Tags = record.Tags.ToArray(),
            Content = record.Content,
            Checksum = record.Checksum,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            TenantId = record.TenantId,
            DocVersion = record.DocVersion,
            EmbeddingSchemaVersion = record.EmbeddingSchemaVersion
        };
    }

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
