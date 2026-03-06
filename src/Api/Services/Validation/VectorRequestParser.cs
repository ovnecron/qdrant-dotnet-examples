using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record UpsertVectorsCommand(
    string CollectionName,
    IReadOnlyList<VectorRecord> Records);

internal sealed record DeleteVectorsCommand(
    string CollectionName,
    IReadOnlyList<string> ChunkIds);

internal interface IVectorRequestParser
{
    bool TryParseDeleteRequest(
        VectorDeleteRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out DeleteVectorsCommand? command,
        out Dictionary<string, string[]> errors);

    bool TryParseUpsertRequest(
        VectorUpsertRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out UpsertVectorsCommand? command,
        out Dictionary<string, string[]> errors);

    bool TryParseSearchRequest(
        VectorSearchRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SearchRequest? searchRequest,
        out Dictionary<string, string[]> errors);
}

internal sealed class VectorRequestParser : IVectorRequestParser
{
    public bool TryParseDeleteRequest(
        VectorDeleteRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out DeleteVectorsCommand? command,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        command = null;

        var collectionName = ResolveCollectionName(request.Collection, defaultCollectionName);
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        var chunkIds = request.ChunkIds
            .Where(static chunkId => !string.IsNullOrWhiteSpace(chunkId))
            .Select(chunkId => chunkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (chunkIds.Length == 0)
        {
            errors["chunkIds"] = ["At least one chunk id is required."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new DeleteVectorsCommand(collectionName, chunkIds);
        return true;
    }

    public bool TryParseUpsertRequest(
        VectorUpsertRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out UpsertVectorsCommand? command,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        command = null;

        var collectionName = ResolveCollectionName(request.Collection, defaultCollectionName);
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        var points = request.Points;
        if (points.Count == 0)
        {
            errors["points"] = ["At least one point is required."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var records = new List<VectorRecord>(points.Count);

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (!TryParsePoint(point, index, now, errors, out var record))
            {
                continue;
            }

            records.Add(record);
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new UpsertVectorsCommand(collectionName, records);
        return true;
    }

    public bool TryParseSearchRequest(
        VectorSearchRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SearchRequest? searchRequest,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        searchRequest = null;

        var collectionName = ResolveCollectionName(request.Collection, defaultCollectionName);
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        var queryVector = request.QueryVector;
        if (queryVector.Count == 0)
        {
            errors["queryVector"] = ["Query vector must contain at least one value."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        searchRequest = new SearchRequest
        {
            CollectionName = collectionName,
            QueryVector = queryVector.ToArray(),
            TopK = request.TopK,
            MinScore = request.MinScore,
            Filter = MapFilter(request.Filter)
        };

        return true;
    }

    private static bool TryParsePoint(
        VectorPointRequest point,
        int index,
        DateTimeOffset now,
        Dictionary<string, string[]> errors,
        [NotNullWhen(true)] out VectorRecord? record)
    {
        var hasErrors = false;

        var id = point.Id?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            errors[$"points[{index}].id"] = ["Point id is required."];
            hasErrors = true;
        }

        var vector = point.Vector;
        if (vector.Count == 0)
        {
            errors[$"points[{index}].vector"] = ["Point vector must contain at least one value."];
            hasErrors = true;
        }

        var payload = point.Payload;
        if (payload is null)
        {
            errors[$"points[{index}].payload"] = ["Point payload is required."];
            hasErrors = true;
        }

        if (hasErrors)
        {
            record = null;
            return false;
        }

        if (payload is null || string.IsNullOrWhiteSpace(id))
        {
            record = null;
            return false;
        }

        var docId = payload.DocId?.Trim();
        if (string.IsNullOrWhiteSpace(docId))
        {
            errors[$"points[{index}].payload.docId"] = ["Payload docId is required."];
            hasErrors = true;
        }

        var source = payload.Source?.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            errors[$"points[{index}].payload.source"] = ["Payload source is required."];
            hasErrors = true;
        }

        var content = payload.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            errors[$"points[{index}].payload.content"] = ["Payload content is required."];
            hasErrors = true;
        }

        if (hasErrors)
        {
            record = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(docId) ||
            string.IsNullOrWhiteSpace(source) ||
            string.IsNullOrWhiteSpace(content))
        {
            record = null;
            return false;
        }

        var createdAt = payload.CreatedAtUtc ?? now;
        var updatedAt = payload.UpdatedAtUtc ?? createdAt;
        var checksum = string.IsNullOrWhiteSpace(payload.Checksum)
            ? ComputeSha256(content)
            : payload.Checksum.Trim();

        record = new VectorRecord
        {
            ChunkId = id,
            Vector = vector.ToArray(),
            DocId = docId,
            Source = source,
            Title = string.IsNullOrWhiteSpace(payload.Title) ? null : payload.Title.Trim(),
            Section = string.IsNullOrWhiteSpace(payload.Section) ? null : payload.Section.Trim(),
            Tags = payload.Tags
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Content = content,
            Checksum = checksum,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            TenantId = string.IsNullOrWhiteSpace(payload.TenantId) ? null : payload.TenantId.Trim(),
            EmbeddingSchemaVersion = string.IsNullOrWhiteSpace(payload.EmbeddingSchemaVersion)
                ? null
                : payload.EmbeddingSchemaVersion.Trim()
        };

        return true;
    }

    private static SearchFilter MapFilter(VectorSearchFilterRequest? filter)
    {
        if (filter is null)
        {
            return new SearchFilter();
        }

        return new SearchFilter
        {
            DocIdEquals = string.IsNullOrWhiteSpace(filter.DocIdEquals) ? null : filter.DocIdEquals.Trim(),
            SourceEquals = string.IsNullOrWhiteSpace(filter.SourceEquals) ? null : filter.SourceEquals.Trim(),
            TagsAny = filter.TagsAny
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            TenantIdEquals = string.IsNullOrWhiteSpace(filter.TenantIdEquals) ? null : filter.TenantIdEquals.Trim(),
            UpdatedAtUtcFrom = filter.UpdatedAtUtcFrom,
            UpdatedAtUtcTo = filter.UpdatedAtUtcTo
        };
    }

    private static string ResolveCollectionName(string? requestedCollection, string defaultCollectionName)
    {
        if (!string.IsNullOrWhiteSpace(requestedCollection))
        {
            return requestedCollection.Trim();
        }

        return defaultCollectionName;
    }

    private static string ComputeSha256(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }
}
