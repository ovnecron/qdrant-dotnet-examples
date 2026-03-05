using System.Globalization;

using Google.Protobuf.Collections;

using Qdrant.Client.Grpc;

using VectorStore.Abstractions.Contracts;

namespace VectorStore.Qdrant.Clients.Internal;

internal static class QdrantPayloadMapper
{
    private const int ContentPreviewLength = 220;

    public static PointStruct MapPoint(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Vector.Count == 0)
        {
            throw new ArgumentException(
                "Vector record must contain at least one vector value.",
                nameof(record));
        }

        var point = new PointStruct
        {
            Id = QdrantPointIdConverter.ToPointId(record.ChunkId),
            Vectors = record.Vector.ToArray()
        };

        point.Payload["chunkId"] = record.ChunkId;
        point.Payload["docId"] = record.DocId;
        point.Payload["source"] = record.Source;
        point.Payload["content"] = record.Content;
        point.Payload["checksum"] = record.Checksum;
        point.Payload["createdAtUtc"] = record.CreatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        point.Payload["updatedAtUtc"] = record.UpdatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(record.Title))
        {
            point.Payload["title"] = record.Title;
        }

        if (!string.IsNullOrWhiteSpace(record.Section))
        {
            point.Payload["section"] = record.Section;
        }

        if (record.Tags.Count > 0)
        {
            point.Payload["tags"] = record.Tags.ToArray();
        }

        if (!string.IsNullOrWhiteSpace(record.TenantId))
        {
            point.Payload["tenantId"] = record.TenantId;
        }

        if (!string.IsNullOrWhiteSpace(record.EmbeddingSchemaVersion))
        {
            point.Payload["embeddingSchemaVersion"] = record.EmbeddingSchemaVersion;
        }

        return point;
    }

    public static SearchResult MapSearchResult(ScoredPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var payload = point.Payload;
        var chunkId = GetPayloadString(payload, "chunkId") ?? QdrantPointIdConverter.ToChunkId(point.Id);
        var content = GetPayloadString(payload, "content") ?? string.Empty;

        return new SearchResult
        {
            ChunkId = chunkId,
            Score = point.Score,
            DocId = GetPayloadString(payload, "docId") ?? chunkId,
            Source = GetPayloadString(payload, "source") ?? string.Empty,
            Title = GetPayloadString(payload, "title"),
            Section = GetPayloadString(payload, "section"),
            Content = content,
            ContentPreview = BuildContentPreview(content),
            Tags = GetPayloadStringArray(payload, "tags"),
            TenantId = GetPayloadString(payload, "tenantId"),
            UpdatedAtUtc = GetPayloadDateTimeOffset(payload, "updatedAtUtc")
        };
    }

    public static VectorRecord MapVectorRecord(RetrievedPoint point, string fallbackChunkId)
    {
        ArgumentNullException.ThrowIfNull(point);

        var payload = point.Payload;
        var chunkId = GetPayloadString(payload, "chunkId") ?? fallbackChunkId;
        var createdAtUtc = GetPayloadDateTimeOffset(payload, "createdAtUtc") ?? DateTimeOffset.UtcNow;
        var updatedAtUtc = GetPayloadDateTimeOffset(payload, "updatedAtUtc") ?? createdAtUtc;

        return new VectorRecord
        {
            ChunkId = chunkId,
            Vector = ExtractVector(point.Vectors),
            DocId = GetPayloadString(payload, "docId") ?? chunkId,
            Source = GetPayloadString(payload, "source") ?? string.Empty,
            Title = GetPayloadString(payload, "title"),
            Section = GetPayloadString(payload, "section"),
            Tags = GetPayloadStringArray(payload, "tags"),
            Content = GetPayloadString(payload, "content") ?? string.Empty,
            Checksum = GetPayloadString(payload, "checksum") ?? string.Empty,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            TenantId = GetPayloadString(payload, "tenantId"),
            EmbeddingSchemaVersion = GetPayloadString(payload, "embeddingSchemaVersion")
        };
    }

    private static IReadOnlyList<float> ExtractVector(VectorsOutput? vectors)
    {
        if (vectors is null)
        {
            return Array.Empty<float>();
        }

        var primaryVector = ExtractDenseVector(vectors.Vector);
        if (primaryVector.Count > 0)
        {
            return primaryVector;
        }

        if (vectors.Vectors is null || vectors.Vectors.Vectors.Count == 0)
        {
            return Array.Empty<float>();
        }

        foreach (var (_, vectorOutput) in vectors.Vectors.Vectors)
        {
            var namedVector = ExtractDenseVector(vectorOutput);
            if (namedVector.Count > 0)
            {
                return namedVector;
            }
        }

        return Array.Empty<float>();
    }

    private static IReadOnlyList<float> ExtractDenseVector(VectorOutput? vectorOutput)
    {
        if (vectorOutput is null)
        {
            return Array.Empty<float>();
        }

        var denseVector = vectorOutput.GetDenseVector();
        if (denseVector is not null && denseVector.Data.Count > 0)
        {
            return denseVector.Data.ToArray();
        }

        return Array.Empty<float>();
    }

    private static string? GetPayloadString(MapField<string, Value> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value))
        {
            return null;
        }

        return ConvertValueToString(value);
    }

    private static IReadOnlyList<string> GetPayloadStringArray(MapField<string, Value> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value))
        {
            return Array.Empty<string>();
        }

        if (value.HasStringValue)
        {
            return new[] { value.StringValue };
        }

        if (value.ListValue is null || value.ListValue.Values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var values = new List<string>(value.ListValue.Values.Count);
        foreach (var item in value.ListValue.Values)
        {
            var converted = ConvertValueToString(item);
            if (!string.IsNullOrWhiteSpace(converted))
            {
                values.Add(converted);
            }
        }

        return values;
    }

    private static DateTimeOffset? GetPayloadDateTimeOffset(MapField<string, Value> payload, string key)
    {
        var raw = GetPayloadString(payload, key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedDateTimeOffset))
        {
            return parsedDateTimeOffset;
        }

        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDateTime))
        {
            return new DateTimeOffset(parsedDateTime, TimeSpan.Zero);
        }

        return null;
    }

    private static string? ConvertValueToString(Value value)
    {
        if (value.HasStringValue)
        {
            return value.StringValue;
        }

        if (value.HasIntegerValue)
        {
            return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.HasDoubleValue)
        {
            return value.DoubleValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.HasBoolValue)
        {
            return value.BoolValue ? "true" : "false";
        }

        return null;
    }

    private static string? BuildContentPreview(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        if (content.Length <= ContentPreviewLength)
        {
            return content;
        }

        return content[..ContentPreviewLength];
    }
}
