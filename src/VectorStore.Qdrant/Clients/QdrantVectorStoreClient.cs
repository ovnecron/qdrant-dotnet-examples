using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Google.Protobuf.Collections;

using Grpc.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

using static Qdrant.Client.Grpc.Conditions;

namespace VectorStore.Qdrant.Clients;

public sealed class QdrantVectorStoreClient : IVectorStoreClient
{
    private const int ContentPreviewLength = 220;

    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStoreClient> _logger;

    public QdrantVectorStoreClient(
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStoreClient> logger,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var resolvedOptions = options.Value
            ?? throw new InvalidOperationException("Qdrant options are not configured.");

        var grpcEndpoint = ResolveGrpcEndpoint(resolvedOptions);
        var timeout = resolvedOptions.Timeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(30)
            : resolvedOptions.Timeout;

        _client = new QdrantClient(grpcEndpoint, resolvedOptions.ApiKey, timeout, loggerFactory);
    }

    public async Task<CollectionInitResult> InitializeCollectionAsync(
        CollectionDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidateCollectionName(definition.Name, nameof(definition.Name));

        if (definition.VectorSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition.VectorSize),
                "Vector size must be greater than zero.");
        }

        var collectionExists = await _client
            .CollectionExistsAsync(definition.Name, cancellationToken)
            .ConfigureAwait(false);

        if (!collectionExists)
        {
            await _client.CreateCollectionAsync(
                    definition.Name,
                    new VectorParams
                    {
                        Size = checked((ulong)definition.VectorSize),
                        Distance = MapDistance(definition.Distance)
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        await EnsurePayloadIndexesAsync(
                definition.Name,
                definition.PayloadIndexes,
                cancellationToken)
            .ConfigureAwait(false);

        return new CollectionInitResult
        {
            CollectionName = definition.Name,
            Created = !collectionExists
        };
    }

    public async Task UpsertAsync(
        string collectionName,
        IReadOnlyCollection<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return;
        }

        var points = records
            .Select(MapPoint)
            .ToArray();

        await _client.UpsertAsync(
                collectionName,
                points,
                wait: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCollectionName(request.CollectionName, nameof(request.CollectionName));

        if (request.QueryVector.Count == 0)
        {
            throw new ArgumentException("Query vector cannot be empty.", nameof(request.QueryVector));
        }

        if (request.TopK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TopK),
                "TopK must be greater than zero.");
        }

        var filter = BuildFilter(request.Filter);

        var hits = await _client.SearchAsync(
                request.CollectionName,
                request.QueryVector.ToArray(),
                filter: filter,
                limit: checked((ulong)request.TopK),
                payloadSelector: true,
                vectorsSelector: false,
                scoreThreshold: request.MinScore,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return hits
            .Select(MapSearchResult)
            .ToArray();
    }

    public async Task<VectorRecord?> GetByIdAsync(
        string collectionName,
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));

        if (string.IsNullOrWhiteSpace(chunkId))
        {
            throw new ArgumentException("Chunk id must be provided.", nameof(chunkId));
        }

        var pointId = ToPointId(chunkId);

        var points = await _client.RetrieveAsync(
                collectionName,
                pointId,
                withPayload: true,
                withVectors: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var point = points.FirstOrDefault();
        return point is null ? null : MapVectorRecord(point, chunkId);
    }

    private async Task EnsurePayloadIndexesAsync(
        string collectionName,
        IReadOnlyList<string> payloadIndexes,
        CancellationToken cancellationToken)
    {
        if (payloadIndexes is null || payloadIndexes.Count == 0)
        {
            return;
        }

        var distinctIndexes = payloadIndexes
            .Where(static index => !string.IsNullOrWhiteSpace(index))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var indexName in distinctIndexes)
        {
            try
            {
                await _client.CreatePayloadIndexAsync(
                        collectionName,
                        indexName,
                        ResolvePayloadSchemaType(indexName),
                        wait: true,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsAlreadyExistsError(exception))
            {
                _logger.LogDebug(
                    exception,
                    "Skipping payload index creation because index '{IndexName}' already exists in '{CollectionName}'.",
                    indexName,
                    collectionName);
            }
        }
    }

    private static PointStruct MapPoint(VectorRecord record)
    {
        if (record.Vector.Count == 0)
        {
            throw new ArgumentException(
                "Vector record must contain at least one vector value.",
                nameof(record));
        }

        var point = new PointStruct
        {
            Id = ToPointId(record.ChunkId),
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

    private static SearchResult MapSearchResult(ScoredPoint point)
    {
        var payload = point.Payload;
        var chunkId = GetPayloadString(payload, "chunkId") ?? ToChunkId(point.Id);
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

    private static VectorRecord MapVectorRecord(RetrievedPoint point, string fallbackChunkId)
    {
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

    private static Filter? BuildFilter(SearchFilter filter)
    {
        var conditions = new List<Condition>();

        if (!string.IsNullOrWhiteSpace(filter.DocIdEquals))
        {
            conditions.Add(MatchKeyword("docId", filter.DocIdEquals));
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceEquals))
        {
            conditions.Add(MatchKeyword("source", filter.SourceEquals));
        }

        if (!string.IsNullOrWhiteSpace(filter.TenantIdEquals))
        {
            conditions.Add(MatchKeyword("tenantId", filter.TenantIdEquals));
        }

        if (filter.TagsAny.Count > 0)
        {
            var tags = filter.TagsAny
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (tags.Length > 0)
            {
                conditions.Add(Match("tags", tags));
            }
        }

        if (filter.UpdatedAtUtcFrom is not null || filter.UpdatedAtUtcTo is not null)
        {
            conditions.Add(DatetimeRange(
                "updatedAtUtc",
                gte: filter.UpdatedAtUtcFrom?.UtcDateTime,
                lte: filter.UpdatedAtUtcTo?.UtcDateTime));
        }

        if (conditions.Count == 0)
        {
            return null;
        }

        var filterDefinition = new Filter();
        foreach (var condition in conditions)
        {
            filterDefinition.Must.Add(condition);
        }

        return filterDefinition;
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

        if (vectorOutput.Data.Count > 0)
        {
            return vectorOutput.Data.ToArray();
        }

        if (vectorOutput.Dense is not null && vectorOutput.Dense.Data.Count > 0)
        {
            return vectorOutput.Dense.Data.ToArray();
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

    private static PointId ToPointId(string chunkId)
    {
        if (ulong.TryParse(
                chunkId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericId))
        {
            return numericId;
        }

        if (Guid.TryParse(chunkId, out var guidId))
        {
            return guidId;
        }

        return CreateStableGuid(chunkId);
    }

    private static string ToChunkId(PointId pointId)
    {
        if (pointId.HasUuid)
        {
            return pointId.Uuid;
        }

        if (pointId.HasNum)
        {
            return pointId.Num.ToString(CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }

    private static Guid CreateStableGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    private static Distance MapDistance(VectorDistance distance)
    {
        return distance switch
        {
            VectorDistance.Cosine => Distance.Cosine,
            VectorDistance.Dot => Distance.Dot,
            VectorDistance.Euclid => Distance.Euclid,
            VectorDistance.Manhattan => Distance.Manhattan,
            _ => throw new ArgumentOutOfRangeException(nameof(distance), distance, "Unsupported vector distance.")
        };
    }

    private static PayloadSchemaType ResolvePayloadSchemaType(string indexName)
    {
        return indexName.Equals("updatedAtUtc", StringComparison.OrdinalIgnoreCase)
            ? PayloadSchemaType.Datetime
            : PayloadSchemaType.Keyword;
    }

    private static bool IsAlreadyExistsError(Exception exception)
    {
        if (exception is RpcException rpcException && rpcException.StatusCode == StatusCode.AlreadyExists)
        {
            return true;
        }

        return exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCollectionName(string collectionName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name must be provided.", parameterName);
        }
    }

    private static Uri ResolveGrpcEndpoint(QdrantOptions options)
    {
        if (TryParseUri(Environment.GetEnvironmentVariable("QDRANT__ENDPOINT_GRPC"), out var endpoint))
        {
            return endpoint;
        }

        if (TryParseUri(options.EndpointGrpc, out endpoint))
        {
            return endpoint;
        }

        var restEndpoint = Environment.GetEnvironmentVariable("QDRANT__ENDPOINT_REST") ?? options.EndpointRest;
        if (TryParseUri(restEndpoint, out var restUri))
        {
            return PromoteRestEndpointToGrpc(restUri);
        }

        throw new InvalidOperationException(
            "Qdrant gRPC endpoint is not configured. Set QDRANT__ENDPOINT_GRPC or Qdrant:EndpointGrpc.");
    }

    private static bool TryParseUri(string? rawValue, [NotNullWhen(true)] out Uri? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(rawValue) &&
            Uri.TryCreate(rawValue, UriKind.Absolute, out var parsedEndpoint) &&
            parsedEndpoint is not null)
        {
            endpoint = parsedEndpoint;
            return true;
        }

        endpoint = null;
        return false;
    }

    private static Uri PromoteRestEndpointToGrpc(Uri restEndpoint)
    {
        var uriBuilder = new UriBuilder(restEndpoint);

        if (uriBuilder.Port is > 0 and < ushort.MaxValue)
        {
            uriBuilder.Port = uriBuilder.Port == 6333 ? 6334 : uriBuilder.Port + 1;
        }

        return uriBuilder.Uri;
    }
}
